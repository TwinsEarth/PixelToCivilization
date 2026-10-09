---
uid: pmdt-mermaid
---

# Mermaid diagrams

To use a [mermaid](https://mermaid.js.org/) diagram on a page, put the diagram text inside a `<pre class="mermaid">diagram</pre>` element.

Here is one mermaid diagram example:

```html
<pre class="mermaid">
        graph TD 
        A[Client] --> B[Load Balancer] 
        B --> C[Server1] 
        B --> D[Server2]
</pre>
```

<pre class="mermaid">
        graph TD 
        A[Client] --> B[Load Balancer] 
        B --> C[Server1] 
        B --> D[Server2]
</pre>

And here is another:

```html
<pre class="mermaid">
        graph TD 
        A[Client] -->|tcp_123| B
        B(Load Balancer) 
        B -->|tcp_456| C[Server1] 
        B -->|tcp_456| D[Server2]
</pre>
```

<pre class="mermaid">
        graph TD 
        A[Client] -->|tcp_123| B
        B(Load Balancer) 
        B -->|tcp_456| C[Server1] 
        B -->|tcp_456| D[Server2]
</pre>

And some post text.