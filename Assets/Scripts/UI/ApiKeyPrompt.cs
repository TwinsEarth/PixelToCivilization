using System;
using UnityEngine;
using UnityEngine.UI;
using PixelToCivilization.Core;

namespace PixelToCivilization.UI
{
    /// <summary>V9.3.10 AI 密钥输入弹窗（仿 Debug 密码门：全屏遮罩 + 居中输入框 + Show 即自动聚焦）。
    /// 用户实测行内 InputField 点击聚焦在 WebGL 下不可靠；弹窗在出现时直接 ActivateInputField，
    /// 配合加载页 canvas 焦点兜底，实体键盘可直接输入。保存/清空后回调由调用方刷新面板。</summary>
    public class ApiKeyPrompt : MonoBehaviour
    {
        public static ApiKeyPrompt Current;
        private GameObject _root;
        private InputField _input;
        private Action<string> _onSave;

        /// <summary>parent 必须传 UICanvas 下的活动节点（HUD），同 DebugPasswordPrompt 约束。</summary>
        public void Show(GameManager gm, Action<string> onSave, Transform parent=null)
        {
            if(Current!=null && Current._root!=null){ Current._onSave=onSave; Current.Activate(); return; }
            Current=this; _onSave=onSave;
            Transform p = parent!=null ? parent : transform;
            _root=UITheme.Panel("ApiKeyPrompt",p,UITheme.HexA(0x000000,0.6f));
            _root.transform.SetAsLastSibling();
            var rt=_root.GetComponent<RectTransform>();rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;
            var box=UITheme.Panel("Box",_root.transform,new Color(0.985f,0.975f,0.935f,1f));
            UITheme.SetOutline(box,UITheme.Gold,1);
            var brt=box.GetComponent<RectTransform>();
            brt.anchorMin=brt.anchorMax=new Vector2(0.5f,0.5f);brt.sizeDelta=new Vector2(430,276);
            var vl=box.AddComponent<VerticalLayoutGroup>();vl.spacing=6;vl.padding=new RectOffset(20,20,16,14);
            vl.childControlWidth=true;vl.childForceExpandWidth=true;vl.childControlHeight=true;vl.childForceExpandHeight=false;
            UITheme.Label("t",box.transform,"AI 密钥设置 · 九神联网议政",18,TextAnchor.MiddleCenter,UITheme.Gold).gameObject.AddComponent<LayoutElement>().preferredHeight=28;
            var cou=gm.Council;
            string keyShow = string.IsNullOrEmpty(cou.ApiKey) ? "未配置·离线规则自治" : "已配置 " + cou.ApiKey.Substring(0, Mathf.Min(5, cou.ApiKey.Length)) + "…";
            UITheme.Label("s",box.transform,"状态："+keyShow+"｜模型 "+cou.Model+"｜"+cou.Endpoint.Replace("https://",""),11,TextAnchor.MiddleLeft,UITheme.Sky).gameObject.AddComponent<LayoutElement>().preferredHeight=20;
            var fieldGo=UITheme.Panel("Input",box.transform,new Color(0.93f,0.96f,0.98f,1f));
            UITheme.SetOutline(fieldGo,UITheme.Gold,1);
            fieldGo.AddComponent<LayoutElement>().preferredHeight=44;
            var input=fieldGo.AddComponent<InputField>();
            var ph=UITheme.Label("ph",fieldGo.transform,"粘贴 DeepSeek API Key（sk-…）",13,TextAnchor.MiddleLeft,UITheme.HexA(0x9aa3c0,1));
            ph.rectTransform.anchorMin=Vector2.zero;ph.rectTransform.anchorMax=Vector2.one;
            ph.rectTransform.offsetMin=new Vector2(12,2);ph.rectTransform.offsetMax=new Vector2(-12,-2);
            var txt=UITheme.Label("txt",fieldGo.transform,"",13,TextAnchor.MiddleLeft,UITheme.Text);
            txt.rectTransform.anchorMin=Vector2.zero;txt.rectTransform.anchorMax=Vector2.one;
            txt.rectTransform.offsetMin=new Vector2(12,2);txt.rectTransform.offsetMax=new Vector2(-12,-2);
            input.targetGraphic=fieldGo.GetComponent<Image>();
            input.textComponent=txt;input.placeholder=ph;
            input.caretColor=Color.white;input.caretWidth=3;input.selectionColor=UITheme.HexA(0xf3d061,0.35f);
            input.interactable=true;
            input.onValueChanged.AddListener(v=>Debug.Log("[AIKey] len="+v.Length));   // V9.3.9 输入探针（浏览器回归观测键盘是否进入 Unity）
            _input=input;
            // V9.3.12 粘贴指引（WebGL 右键菜单/Ctrl+V 不可靠：点下方按钮经 JS 剪贴板桥读取系统剪贴板）
            UITheme.Label("pt",box.transform,"WebGL 无法右键粘贴？点下方「读取剪贴板」按钮，或聚焦输入框后直接 Ctrl+V（已自动兜底）。",10,TextAnchor.MiddleLeft,UITheme.Sky).gameObject.AddComponent<LayoutElement>().preferredHeight=18;
            var row=UITheme.Panel("Row",box.transform,new Color(0,0,0,0));
            row.AddComponent<LayoutElement>().preferredHeight=42;
            var h=row.AddComponent<HorizontalLayoutGroup>();h.spacing=6;h.childControlWidth=true;h.childForceExpandWidth=true;h.childControlHeight=true;
            var paste=UITheme.Btn("paste",row.transform,"读取剪贴板",12,UITheme.HexA(0x34aef0,0.22f));
            paste.onClick.AddListener(()=>{ try{ Application.ExternalCall("PXC_ReadClipboard"); }catch(System.Exception ex){ Debug.Log("[AIKey] ExternalCall fail "+ex.Message); } });
            var ok=UITheme.Btn("ok",row.transform,"保存 · 即时联网",12,UITheme.HexA(0xFFD700,0.25f));
            var clear=UITheme.Btn("clear",row.transform,"清空 · 离线自治",12);
            var cancel=UITheme.Btn("cancel",row.transform,"取消",12);
            ok.onClick.AddListener(()=>{ cou.SetApiKey(_input.text); _onSave?.Invoke(_input.text); Close(); UIManager.Instance?.Toast("AI 密钥已"+(string.IsNullOrEmpty(cou.ApiKey)?"清空·离线自治":"保存·即时联网")); });
            clear.onClick.AddListener(()=>{ cou.SetApiKey(""); _onSave?.Invoke(""); Close(); UIManager.Instance?.Toast("AI 密钥已清空 · 离线规则自治"); });
            cancel.onClick.AddListener(()=>{ Close(); });
            Activate();
        }
        /// <summary>V9.3.10 聚焦输入框（弹窗出现即聚焦，实体键盘可直接输入）</summary>
        public void Activate(){ if(_input!=null){ _input.Select(); _input.ActivateInputField(); } }
        /// <summary>V9.3.10 程序化赋值（浏览器回归验证输入链：触发 onValueChanged→[AIKey] 探针日志）</summary>
        public void SetText(string v){ if(_input!=null){ _input.text=v; } }
        public void Close(){ if(_root!=null) Destroy(_root); _root=null; Current=null; }
    }
}
