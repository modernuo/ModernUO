import { type ReactNode, useEffect, useState } from 'react';
import Link from '@docusaurus/Link';
import styles from './styles.module.css';

function formatCount(n: number): string {
  if (n >= 1000) {
    return `${(n / 1000).toFixed(1).replace(/\.0$/, '')}k`;
  }
  return n.toString();
}

type Counts = {
  stars?: number;
  discord?: number;
};

function useSocialCounts(): Counts {
  const [counts, setCounts] = useState<Counts>({});

  useEffect(() => {
    let cancelled = false;

    async function fetchCounts() {
      const results: Counts = {};

      try {
        const res = await fetch('https://api.github.com/repos/modernuo/ModernUO');
        if (res.ok) {
          const data = await res.json();
          results.stars = data.stargazers_count;
        }
      } catch { /* silent */ }

      try {
        const res = await fetch('https://discord.com/api/v9/invites/DHkNUsq?with_counts=true');
        if (res.ok) {
          const data = await res.json();
          results.discord = data.approximate_member_count;
        }
      } catch { /* silent */ }

      if (!cancelled) {
        setCounts(results);
      }
    }

    fetchCounts();
    return () => { cancelled = true; };
  }, []);

  return counts;
}

type SocialItem = {
  label: string;
  href: string;
  icon: ReactNode;
  countKey?: keyof Counts;
};

const socialLinks: SocialItem[] = [
  {
    label: 'GitHub',
    href: 'https://github.com/modernuo/ModernUO',
    countKey: 'stars',
    icon: (
      <svg viewBox="0 0 24 24" fill="currentColor" width="16" height="16">
        <path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z" />
      </svg>
    ),
  },
  {
    label: 'Discord',
    href: 'https://muo.gg/discord',
    countKey: 'discord',
    icon: (
      <svg viewBox="0 0 24 24" fill="currentColor" width="16" height="16">
        <path d="M20.317 4.37a19.791 19.791 0 0 0-4.885-1.515.074.074 0 0 0-.079.037c-.21.375-.444.864-.608 1.25a18.27 18.27 0 0 0-5.487 0 12.64 12.64 0 0 0-.617-1.25.077.077 0 0 0-.079-.037A19.736 19.736 0 0 0 3.677 4.37a.07.07 0 0 0-.032.027C.533 9.046-.32 13.58.099 18.057a.082.082 0 0 0 .031.057 19.9 19.9 0 0 0 5.993 3.03.078.078 0 0 0 .084-.028c.462-.63.874-1.295 1.226-1.994a.076.076 0 0 0-.041-.106 13.107 13.107 0 0 1-1.872-.892.077.077 0 0 1-.008-.128 10.2 10.2 0 0 0 .372-.292.074.074 0 0 1 .077-.01c3.928 1.793 8.18 1.793 12.062 0a.074.074 0 0 1 .078.01c.12.098.246.198.373.292a.077.077 0 0 1-.006.127 12.299 12.299 0 0 1-1.873.892.077.077 0 0 0-.041.107c.36.698.772 1.362 1.225 1.993a.076.076 0 0 0 .084.028 19.839 19.839 0 0 0 6.002-3.03.077.077 0 0 0 .032-.054c.5-5.177-.838-9.674-3.549-13.66a.061.061 0 0 0-.031-.03zM8.02 15.33c-1.183 0-2.157-1.085-2.157-2.419 0-1.333.956-2.419 2.157-2.419 1.21 0 2.176 1.096 2.157 2.42 0 1.333-.956 2.418-2.157 2.418zm7.975 0c-1.183 0-2.157-1.085-2.157-2.419 0-1.333.956-2.419 2.157-2.419 1.21 0 2.176 1.096 2.157 2.42 0 1.333-.947 2.418-2.157 2.418z" />
      </svg>
    ),
  },
  {
    label: 'Reddit',
    href: 'https://muo.gg/reddit',
    icon: (
      <svg viewBox="0 0 24 24" fill="currentColor" width="16" height="16">
        <path d="M12 0A12 12 0 0 0 0 12a12 12 0 0 0 12 12 12 12 0 0 0 12-12A12 12 0 0 0 12 0zm5.01 4.744c.688 0 1.25.561 1.25 1.249a1.25 1.25 0 0 1-2.498.056l-2.597-.547-.8 3.747c1.824.07 3.48.632 4.674 1.488.308-.309.73-.491 1.207-.491.968 0 1.754.786 1.754 1.754 0 .716-.435 1.333-1.01 1.614a3.111 3.111 0 0 1 .042.52c0 2.694-3.13 4.87-7.004 4.87-3.874 0-7.004-2.176-7.004-4.87 0-.183.015-.366.043-.534A1.748 1.748 0 0 1 4.028 12c0-.968.786-1.754 1.754-1.754.463 0 .898.196 1.207.49 1.207-.883 2.878-1.43 4.744-1.487l.885-4.182a.342.342 0 0 1 .14-.197.35.35 0 0 1 .238-.042l2.906.617a1.214 1.214 0 0 1 1.108-.701zM9.25 12C8.561 12 8 12.562 8 13.25c0 .687.561 1.248 1.25 1.248.687 0 1.248-.561 1.248-1.249 0-.688-.561-1.249-1.249-1.249zm5.5 0c-.687 0-1.248.561-1.248 1.25 0 .687.561 1.248 1.249 1.248.688 0 1.249-.561 1.249-1.249 0-.687-.562-1.249-1.25-1.249zm-5.466 3.99a.327.327 0 0 0-.231.094.33.33 0 0 0 0 .463c.842.842 2.484.913 2.961.913.477 0 2.105-.056 2.961-.913a.361.361 0 0 0 .029-.463.33.33 0 0 0-.464 0c-.547.533-1.684.73-2.512.73-.828 0-1.979-.196-2.512-.73a.326.326 0 0 0-.232-.095z" />
      </svg>
    ),
  },
  {
    label: 'Sponsor',
    href: 'https://github.com/sponsors/modernuo',
    icon: (
      <svg viewBox="0 0 24 24" fill="currentColor" width="16" height="16">
        <path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z" />
      </svg>
    ),
  },
];

const docLinks = [
  { label: 'Get Started', to: '/docs/getting-started/installation' },
  { label: 'Configuration', to: '/docs/getting-started/configuration' },
  { label: 'Content Development', to: '/docs/development/items-and-mobiles' },
  { label: 'Commands', href: '/commands.html' },
  { label: 'Packets', href: '/packets.html' },
];

export default function Footer(): ReactNode {
  const counts = useSocialCounts();

  return (
    <footer className={styles.footer}>
      <div className="container">
        <div className={styles.content}>
          <div className={styles.docs}>
            <h4 className={styles.heading}>Documentation</h4>
            <ul className={styles.linkList}>
              {docLinks.map((link, idx) => (
                <li key={idx}>
                  {'to' in link ? (
                    <Link to={link.to} className={styles.docLink}>{link.label}</Link>
                  ) : (
                    <a href={link.href} className={styles.docLink}>{link.label}</a>
                  )}
                </li>
              ))}
            </ul>
          </div>
          <div className={styles.social}>
            <h4 className={styles.heading}>Community</h4>
            <div className={styles.socialButtons}>
              {socialLinks.map((link, idx) => {
                const count = link.countKey ? counts[link.countKey] : undefined;
                return (
                  <a
                    key={idx}
                    href={link.href}
                    target="_blank"
                    rel="noopener noreferrer"
                    className={styles.socialLink}
                  >
                    <span className={styles.icon}>{link.icon}</span>
                    <span>{link.label}</span>
                    {count != null && (
                      <span className={styles.count}>{formatCount(count)}</span>
                    )}
                  </a>
                );
              })}
            </div>
          </div>
        </div>
        <div className={styles.bottom}>
          <span>Copyright {new Date().getFullYear()} ModernUO Development Team</span>
        </div>
      </div>
    </footer>
  );
}
