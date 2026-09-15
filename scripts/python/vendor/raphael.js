/* Project Health's local Treant-compatible renderer does not require Raphael.
 * Preserve the historical global for compatibility with the upstream page contract.
 */
window.Raphael = window.Raphael || function Raphael(){ return {}; };
