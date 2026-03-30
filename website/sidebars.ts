import type { SidebarsConfig } from '@docusaurus/plugin-content-docs';

const sidebars: SidebarsConfig = {
  docsSidebar: [
    {
      type: 'category',
      label: 'Get Started',
      collapsed: false,
      items: [
        'getting-started/installation',
        'getting-started/building',
        'getting-started/starting',
        'getting-started/configuration',
      ],
    },
    {
      type: 'category',
      label: 'Content Development',
      collapsed: false,
      items: [
        'development/items-and-mobiles',
        'development/serialization',
        'development/timers',
        'development/commands-and-targeting',
        'development/era-and-expansions',
      ],
    },
    {
      type: 'category',
      label: 'Reference',
      collapsed: false,
      items: [
        {
          type: 'link',
          label: 'Commands',
          href: 'pathname:///commands.html',
          className: 'menu__link--internal',
        },
        {
          type: 'link',
          label: 'Packets',
          href: 'pathname:///packets.html',
          className: 'menu__link--internal',
        },
      ],
    },
  ],
};

export default sidebars;
