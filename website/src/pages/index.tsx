import type { ReactNode } from 'react';
import Link from '@docusaurus/Link';
import Layout from '@theme/Layout';
import HomepageFeatures from '@site/src/components/HomepageFeatures';
import QuickStart from '@site/src/components/QuickStart';

import styles from './index.module.css';

function HomepageHeader() {
  return (
    <header className={styles.hero}>
      <div className="container">
        <div className={styles.logoWrapper}>
          <div className={styles.glow} />
          <img
            src="/branding/logo.svg"
            alt="ModernUO"
            className={styles.logo}
          />
        </div>
        <p className={styles.tagline}>
          The Ultima Online server emulator for the modern era
        </p>
        <div className={styles.buttons}>
          <Link
            className="button button--lg button--gold"
            to="/docs/getting-started/installation"
          >
            Get Started
          </Link>
        </div>
      </div>
    </header>
  );
}

export default function Home(): ReactNode {
  return (
    <Layout
      title="Ultima Online Server Emulator"
      description="ModernUO is a modern, open-source Ultima Online server emulator built on .NET 10. High performance, code-generated serialization, and an active community."
    >
      <HomepageHeader />
      <main>
        <HomepageFeatures />
        <QuickStart />
      </main>
    </Layout>
  );
}
