import type { ReactNode } from 'react';
import CodeBlock from '@theme/CodeBlock';
import Link from '@docusaurus/Link';
import OsTabs from '@site/src/components/OsTabs';
import styles from './styles.module.css';

export default function QuickStart(): ReactNode {
  return (
    <section className={styles.quickStart}>
      <div className="container">
        <div className={styles.content}>
          <h2 className={styles.heading}>Up and running in minutes</h2>
          <p className={styles.subheading}>
            Clone, build, and launch your server with three commands.
          </p>
          <div className={styles.codeWrapper}>
            <OsTabs>
              {{
                linux: (
                  <CodeBlock language="bash">
                    {`git clone https://github.com/modernuo/modernuo\ncd modernuo\n./publish.sh release linux x64`}
                  </CodeBlock>
                ),
                macos: (
                  <CodeBlock language="bash">
                    {`git clone https://github.com/modernuo/modernuo\ncd modernuo\n./publish.sh release osx x64`}
                  </CodeBlock>
                ),
                windows: (
                  <CodeBlock language="bash">
                    {`git clone https://github.com/modernuo/modernuo\ncd modernuo\n./publish.cmd release win x64`}
                  </CodeBlock>
                ),
              }}
            </OsTabs>
          </div>
          <Link
            className="button button--lg button--outline-gold"
            to="/docs/getting-started/installation"
          >
            View full setup guide
          </Link>
        </div>
      </div>
    </section>
  );
}
