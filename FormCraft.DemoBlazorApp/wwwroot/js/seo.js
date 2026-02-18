// SEO Helper for Blazor WebAssembly
window.seoHelper = {
    updateMetaTags: function(title, description, url) {
        // Update title
        document.title = title + " | FormCraft";
        
        // Update meta description
        let metaDescription = document.querySelector('meta[name="description"]');
        if (metaDescription) {
            metaDescription.content = description;
        }
        
        // Update canonical URL
        let canonical = document.querySelector('link[rel="canonical"]');
        if (canonical) {
            canonical.href = url;
        }
        
        // Update Open Graph tags
        let ogTitle = document.querySelector('meta[property="og:title"]');
        if (ogTitle) {
            ogTitle.content = title;
        }
        
        let ogDescription = document.querySelector('meta[property="og:description"]');
        if (ogDescription) {
            ogDescription.content = description;
        }
        
        let ogUrl = document.querySelector('meta[property="og:url"]');
        if (ogUrl) {
            ogUrl.content = url;
        }
        
        // Update Twitter tags
        let twitterTitle = document.querySelector('meta[property="twitter:title"]');
        if (twitterTitle) {
            twitterTitle.content = title;
        }
        
        let twitterDescription = document.querySelector('meta[property="twitter:description"]');
        if (twitterDescription) {
            twitterDescription.content = description;
        }
        
        let twitterUrl = document.querySelector('meta[property="twitter:url"]');
        if (twitterUrl) {
            twitterUrl.content = url;
        }
    },
    
    // Pre-render content for search engines
    addPrerenderedContent: function(content) {
        let prerenderedDiv = document.getElementById('prerendered-content');
        if (!prerenderedDiv) {
            prerenderedDiv = document.createElement('div');
            prerenderedDiv.id = 'prerendered-content';
            prerenderedDiv.style.display = 'none';
            prerenderedDiv.setAttribute('aria-hidden', 'true');
            document.body.appendChild(prerenderedDiv);
        }
        prerenderedDiv.innerHTML = content;
    }
};