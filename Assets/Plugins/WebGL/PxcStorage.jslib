// V9.7.1 混合存档 —— 存档正文独立 IndexedDB 存储层
// 设计：启动时全量预载到内存缓存（C# 同步读写，语义与 PlayerPrefs 一致），
//       写操作进入缓存 + 防抖落库；PxcFlushBody 立即落库（存档关键节点）；
//       IndexedDB 不可用（隐私模式等）时降级为纯内存缓存，不阻断游戏。
mergeInto(LibraryManager.library, {

  PxcInitDb: function () {
    try {
      if (window.__pxcReady) { SendMessage('GameManager', 'OnStorageReady', 'ready'); return; }
      if (window.__pxcIniting) return;
      window.__pxcIniting = true;
      window.__pxcCache = {};
      window.__pxcPending = {};   // 'put:key' / 'del:key'
      window.__pxcDb = null;
      window.__pxcFlushTimer = null;

      // 内部：把 pending 操作一次性落库（单个事务，保证同批写入原子性）
      window.__pxcFlushBody = function () {
        try {
          if (window.__pxcFlushTimer) { clearTimeout(window.__pxcFlushTimer); window.__pxcFlushTimer = null; }
          var db = window.__pxcDb;
          var pending = window.__pxcPending || {};
          var keys = Object.keys(pending);
          if (!db || keys.length === 0) { if (keys.length) window.__pxcPending = {}; return; }
          window.__pxcPending = {};
          var tx = db.transaction('body', 'readwrite');
          var store = tx.objectStore('body');
          for (var i = 0; i < keys.length; i++) {
            var op = keys[i];
            if (op.indexOf('put:') === 0) store.put(pending[op], op.substring(4));
            else if (op.indexOf('del:') === 0) store.delete(op.substring(4));
          }
          tx.onabort = function (e) { console.warn('[PxcStorage] flush aborted', e); };
          tx.onerror = function (e) { console.warn('[PxcStorage] flush error', e); };
        } catch (e) { console.error('[PxcStorage] flush fatal', e); }
      };

      var finishReady = function () {
        window.__pxcReady = true;
        window.__pxcIniting = false;
        try { SendMessage('GameManager', 'OnStorageReady', 'ready'); } catch (e) { }
      };

      if (typeof indexedDB === 'undefined') { console.warn('[PxcStorage] IndexedDB unavailable, memory-only'); finishReady(); return; }

      var req;
      try { req = indexedDB.open('PxC_SaveBody', 1); } catch (e) { console.warn('[PxcStorage] open failed, memory-only', e); finishReady(); return; }

      req.onupgradeneeded = function (e) {
        var db = e.target.result;
        if (!db.objectStoreNames.contains('body')) db.createObjectStore('body');
      };
      req.onsuccess = function (e) {
        var db = e.target.result;
        window.__pxcDb = db;
        var tx = db.transaction('body', 'readonly');
        var store = tx.objectStore('body');
        var keysReq = store.getAllKeys();
        var valsReq = store.getAll();
        // V9.7.3 修复：getAllKeys 与 getAll 是两个独立异步请求，旧代码在 keysReq.onsuccess 中
        // 直接读 valsReq.result（此时 vals 常未完成），抛 InvalidStateError 导致整个预载循环中断、
        // 启动缓存为空、所有旧存档读不到。这里改为分别等两个 onsuccess，都完成后再合并。
        var gotKeys = false, gotVals = false, keyData = [], valData = [];
        function preloadFinish() {
          if (!(gotKeys && gotVals)) return;
          try {
            for (var i = 0; i < keyData.length; i++)
              window.__pxcCache[String(keyData[i])] = (valData[i] === undefined ? '' : String(valData[i]));
          } catch (err) { console.warn('[PxcStorage] preload error', err); }
          // 页面隐藏时尽力落库，降低强杀/崩溃丢档概率
          document.addEventListener('visibilitychange', function () {
            if (document.visibilityState === 'hidden') window.__pxcFlushBody && window.__pxcFlushBody();
          });
          window.addEventListener('beforeunload', function () {
            window.__pxcFlushBody && window.__pxcFlushBody();
          });
          finishReady();
        }
        keysReq.onsuccess = function () { gotKeys = true; keyData = keysReq.result || []; preloadFinish(); };
        valsReq.onsuccess = function () { gotVals = true; valData = valsReq.result || []; preloadFinish(); };
        keysReq.onerror = function () { gotKeys = true; console.warn('[PxcStorage] preload keys failed'); preloadFinish(); };
        valsReq.onerror = function () { gotVals = true; console.warn('[PxcStorage] preload vals failed'); preloadFinish(); };
      };
      req.onerror = function () { console.warn('[PxcStorage] IndexedDB open error, memory-only'); finishReady(); };
      // 极端环境 4 秒未回调 = 降级，不卡死启动
      setTimeout(function () { if (!window.__pxcReady) { console.warn('[PxcStorage] init timeout, memory-only'); finishReady(); } }, 4000);
    } catch (e) {
      console.error('[PxcStorage] PxcInitDb fatal', e);
      window.__pxcReady = true;
      try { SendMessage('GameManager', 'OnStorageReady', 'ready'); } catch (e2) { }
    }
  },

  PxcGetBody: function (keyPtr) {
    var key = UTF8ToString(keyPtr);
    var v = (window.__pxcCache && Object.prototype.hasOwnProperty.call(window.__pxcCache, key)) ? window.__pxcCache[key] : '';
    if (v === null || v === undefined) v = '';
    var bytes = lengthBytesUTF8(v) + 1;
    var buf = _malloc(bytes);
    stringToUTF8(v, buf, bytes);
    return buf;
  },

  PxcSetBody: function (keyPtr, valPtr) {
    var key = UTF8ToString(keyPtr);
    var val = UTF8ToString(valPtr);
    if (!window.__pxcCache) window.__pxcCache = {};
    window.__pxcCache[key] = val;
    if (!window.__pxcPending) window.__pxcPending = {};
    window.__pxcPending['put:' + key] = val;
    if (window.__pxcFlushTimer) clearTimeout(window.__pxcFlushTimer);
    window.__pxcFlushTimer = setTimeout(function () { window.__pxcFlushBody && window.__pxcFlushBody(); }, 500);
  },

  PxcDeleteBody: function (keyPtr) {
    var key = UTF8ToString(keyPtr);
    if (window.__pxcCache) delete window.__pxcCache[key];
    if (!window.__pxcPending) window.__pxcPending = {};
    window.__pxcPending['del:' + key] = 1;
    if (window.__pxcFlushTimer) clearTimeout(window.__pxcFlushTimer);
    window.__pxcFlushTimer = setTimeout(function () { window.__pxcFlushBody && window.__pxcFlushBody(); }, 500);
  },

  PxcHasBody: function (keyPtr) {
    var key = UTF8ToString(keyPtr);
    return (window.__pxcCache && Object.prototype.hasOwnProperty.call(window.__pxcCache, key)) ? 1 : 0;
  },

  PxcFlushBody: function () {
    if (window.__pxcFlushBody) window.__pxcFlushBody();
  },

  PxcFreeBuf: function (ptr) {
    try { if (ptr) _free(ptr); } catch (e) { }
  }
});
