interface TurnstileRenderOptions {
  sitekey: string;
  size?: 'invisible' | 'normal' | 'compact';
  callback?: (token: string) => void;
  'error-callback'?: (error: unknown) => void;
}

interface Window {
  turnstile?: {
    render: (container: string | HTMLElement, options: TurnstileRenderOptions) => string;
    remove: (widgetId: string) => void;
    reset?: (widgetId?: string) => void;
  };
}
