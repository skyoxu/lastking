/* Minimal local Treant-compatible renderer for Project Health.
 * Keeps the same new Treant({chart,nodeStructure}) surface without CDN/runtime dependencies.
 */
;(function (exports) {
  function renderNode(node) {
    var wrapper = document.createElement('div');
    wrapper.className = 'node ' + (node.HTMLclass || '');
    if (node.HTMLid) wrapper.id = node.HTMLid;
    var label = document.createElement('div');
    label.className = 'node-name';
    label.textContent = node.text && node.text.name ? node.text.name : '';
    wrapper.appendChild(label);
    if (node.children && node.children.length) {
      var children = document.createElement('div');
      children.className = 'scene-map-branch';
      node.children.forEach(function (child) { children.appendChild(renderNode(child)); });
      wrapper.appendChild(children);
    }
    return wrapper;
  }
  function Treant(config) {
    var chart = config && config.chart || {};
    var container = document.querySelector(chart.container || '');
    if (!container) throw new Error('Treant container not found');
    container.replaceChildren();
    container.classList.add('Treant', 'Treant-loaded');
    if (config.nodeStructure) container.appendChild(renderNode(config.nodeStructure));
    this.destroy = function () { container.replaceChildren(); };
    return this;
  }
  exports.Treant = Treant;
})(window);
