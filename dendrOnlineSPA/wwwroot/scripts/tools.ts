

export const Tools = {

  isCtrlAlt(k: string, e: KeyboardEvent): boolean {
    console.log(`edit keydown ctrl:>${e.ctrlKey}<, alt:>${e.altKey}< key:>${e.key}<`, e);


    const hasCtrl = e.ctrlKey;
    const hasAlt = e.altKey;
    const hasMeta = e.metaKey;
    const hasAltGraph = e.getModifierState && e.getModifierState('AltGraph');

    // We want Ctrl+Alt+E on Windows/Linux.
    // On some layouts, AltGr acts like Ctrl+Alt (so treat AltGraph as Ctrl+Alt).
    const ctrlAltCombo = (hasCtrl && hasAlt) || hasAltGraph;

    // Prefer physical key position for 'E'
    const isKeyVByCode = e.code === `Key${k.toUpperCase()}`;
    // Fallback: some environments may not set code; also allow 'e'/'E'
    const isKeyVByKey = (e.key === k.toLowerCase() || e.key === k.toUpperCase());

    return ctrlAltCombo && (isKeyVByCode || isKeyVByKey);
  },

  getShortcutSelector(shortcuts: string[]): (e: KeyboardEvent) => boolean {
    
    const shortcutCombinaisions: { hasCtrl: boolean, hasAlt: boolean, hasMeta: boolean, hasShift: boolean, hasAltGraph: boolean, key: string }[] = []; 
    for (const shortcut of shortcuts) {
      const parts = shortcut.split('+');
      const hasCtrl = parts.includes('Ctrl');
      const hasAlt = parts.includes('Alt');
      const hasMeta = parts.includes('Meta');
      const hasShift = parts.includes('Shift');
      const hasAltGraph = parts.includes('AltGraph');
      const key = parts[parts.length - 1];
      shortcutCombinaisions.push({ hasCtrl, hasAlt, hasMeta, hasShift, hasAltGraph, key });
    }

    
    const selector = (e: KeyboardEvent) => {
      
      let matched = false;

      for (const shortcut of shortcutCombinaisions) {
        if (shortcut.hasShift !== e.shiftKey) continue;
        if (shortcut.hasCtrl !== e.ctrlKey) continue;
        if (shortcut.hasAlt !== e.altKey) continue;
        if (shortcut.hasMeta !== e.metaKey) continue;
        const isKeyVByCode = e.code === `Key${shortcut.key.toUpperCase()}`;
        const isKeyVByKey = (e.key === shortcut.key.toLowerCase() || e.key === shortcut.key.toUpperCase());
        if (!isKeyVByCode && !isKeyVByKey) continue;
        matched = true;
        break;
      }

      return matched;      
      
    }

    const setShortcuts({shortcut: string, callback() => ()})


    return selector;
  }


}