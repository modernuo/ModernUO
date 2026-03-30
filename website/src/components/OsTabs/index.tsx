import type { ReactNode } from 'react';
import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

function detectOs(): string {
  if (typeof navigator === 'undefined') {
    return 'linux';
  }

  const platform = (navigator.platform ?? '').toLowerCase();
  const ua = navigator.userAgent.toLowerCase();

  if (platform.startsWith('win') || ua.includes('windows')) {
    return 'windows';
  }
  if (platform.startsWith('mac') || ua.includes('macintosh')) {
    return 'macos';
  }
  return 'linux';
}

type OsTabsProps = {
  children: {
    windows?: ReactNode;
    macos?: ReactNode;
    linux?: ReactNode;
  };
};

export default function OsTabs({ children }: OsTabsProps): ReactNode {
  const defaultOs = detectOs();

  return (
    <Tabs groupId="os" defaultValue={defaultOs}>
      {children.windows && (
        <TabItem value="windows" label="Windows">
          {children.windows}
        </TabItem>
      )}
      {children.macos && (
        <TabItem value="macos" label="macOS">
          {children.macos}
        </TabItem>
      )}
      {children.linux && (
        <TabItem value="linux" label="Linux">
          {children.linux}
        </TabItem>
      )}
    </Tabs>
  );
}
