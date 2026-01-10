/**
 * Interactive Examples Component
 * Handles view toggling, fullscreen mode, and markdown syntax highlighting
 * for .interactive-example elements across the website.
 */

(function() {
    'use strict';

    /**
     * Initialize all interactive examples on the page
     */
    function initInteractiveExamples() {
        document.querySelectorAll('.interactive-example').forEach(example => {
            setupViewToggle(example);
            setupFullscreen(example);
        });
        
        highlightMarkdown();
    }

    /**
     * Setup view toggle functionality (Rendered/Source)
     */
    function setupViewToggle(example) {
        const toggles = example.querySelectorAll('.toggle-btn');
        const panes = example.querySelectorAll('.view-pane');

        toggles.forEach(btn => {
            btn.addEventListener('click', () => {
                const view = btn.dataset.view;
                
                // Update buttons
                toggles.forEach(b => b.classList.remove('active'));
                btn.classList.add('active');
                
                // Update panes
                panes.forEach(p => {
                    p.classList.remove('active');
                    if (p.classList.contains(view + '-view')) {
                        p.classList.add('active');
                    }
                });
            });
        });
    }

    /**
     * Setup fullscreen toggle functionality
     */
    function setupFullscreen(example) {
        const fullscreenBtn = example.querySelector('.fullscreen-btn');
        
        if (!fullscreenBtn) return;

        fullscreenBtn.addEventListener('click', () => {
            example.classList.toggle('fullscreen');
            fullscreenBtn.textContent = example.classList.contains('fullscreen') ? '✕' : '⛶';
            document.body.style.overflow = example.classList.contains('fullscreen') ? 'hidden' : '';
        });
    }

    /**
     * Simple Markdown Syntax Highlighter
     * Applies syntax highlighting to markdown source in .source-view elements
     */
    function highlightMarkdown() {
        document.querySelectorAll('.source-view code').forEach(block => {
            let html = block.innerHTML;
            
            // HTML tags (already escaped)
            html = html.replace(/(&lt;[^&]+&gt;)/g, '<span class="md-tag">$1</span>');
            
            // Comments
            html = html.replace(/(&lt;!--[\s\S]*?--&gt;)/g, '<span class="md-comment">$1</span>');
            
            // Headings
            html = html.replace(/^(#{1,6} .+$)/gm, '<span class="md-heading">$1</span>');
            
            // Bold
            html = html.replace(/(\*\*[^*]+\*\*)/g, '<span class="md-bold">$1</span>');
            
            // Inline Code (backticks)
            html = html.replace(/(`[^`]+`)/g, '<span class="md-code">$1</span>');
            
            // Links
            html = html.replace(/(\[[^\]]+\]\([^)]+\))/g, '<span class="md-link">$1</span>');
            
            // Lists
            html = html.replace(/^(\s*[-*+] )/gm, '<span class="md-list">$1</span>');
            
            // Tables (pipe separators)
            html = html.replace(/(\|)/g, '<span class="md-table-sep">$1</span>');
            
            block.innerHTML = html;
        });
    }

    /**
     * Align comparison section heights
     * Makes "without" side match the height of "with" side
     */
    function alignComparisonHeights() {
        document.querySelectorAll('.feature-comparison').forEach(comparison => {
            const columns = comparison.querySelectorAll('.comparison-column');
            if (columns.length !== 2) return;

            const withoutCol = columns[0];
            const withCol = columns[1];
            const withBlock = withCol.querySelector('.code-block');
            
            if (!withBlock) return;

            // Get the natural height of the "with" side
            const withHeight = withBlock.offsetHeight;
            
            // Set both blocks to this height
            withoutCol.querySelector('.code-block').style.height = withHeight + 'px';
            withBlock.style.height = withHeight + 'px';
        });
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            initInteractiveExamples();
            alignComparisonHeights();
        });
    } else {
        initInteractiveExamples();
        alignComparisonHeights();
    }
})();
