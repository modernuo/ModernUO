import type { ReactNode } from 'react';
import Heading from '@theme/Heading';
import styles from './styles.module.css';

type FeatureItem = {
  title: string;
  icon: string;
  description: ReactNode;
};

const FeatureList: FeatureItem[] = [
  {
    title: 'Modern .NET Platform',
    icon: '\u26A1',
    description: (
      <>
        Built on the latest .NET with native Linux support and cross-platform
        compatibility out of the box. OS-level networking and minimal memory
        overhead keep your shard lean and responsive.
      </>
    ),
  },
  {
    title: 'Code-Generated Serialization',
    icon: '\uD83D\uDD27',
    description: (
      <>
        Automatic persistence powered by C# source generators. Annotate your
        fields and get version migrations, dirty tracking, and zero-boilerplate
        world saves&mdash;no manual serialization code required.
      </>
    ),
  },
  {
    title: 'Active Development & Community',
    icon: '\uD83D\uDC65',
    description: (
      <>
        Actively maintained with regular updates and a growing contributor base.
        Get help on Discord, collaborate on GitHub, and be part of a community
        that&rsquo;s pushing UO emulation forward.
      </>
    ),
  },
  {
    title: 'Built for Performance at Scale',
    icon: '\uD83D\uDE80',
    description: (
      <>
        Engineered for large shards. Optimized data structures, a lock-free
        architecture, and parallel world saves ensure your server stays smooth
        under heavy player load.
      </>
    ),
  },
];

function Feature({ title, icon, description }: FeatureItem) {
  return (
    <div className={styles.featureCard}>
      <div className={styles.featureHeader}>
        <span className={styles.featureIcon}>{icon}</span>
        <Heading as="h3">{title}</Heading>
      </div>
      <p>{description}</p>
    </div>
  );
}

export default function HomepageFeatures(): ReactNode {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className={styles.grid}>
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
