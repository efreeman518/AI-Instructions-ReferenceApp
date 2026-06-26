(function () {
  const root = document.documentElement;
  const button = document.querySelector('[data-theme-toggle]');
  const storedTheme = localStorage.getItem('taskflow-tech-design-theme');
  const preferredTheme = window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark';

  setTheme(storedTheme || preferredTheme);

  button?.addEventListener('click', () => {
    setTheme(root.dataset.theme === 'dark' ? 'light' : 'dark');
  });

  function setTheme(theme) {
    root.dataset.theme = theme;
    localStorage.setItem('taskflow-tech-design-theme', theme);
    if (button) button.textContent = theme === 'dark' ? 'Light' : 'Dark';
  }
})();
