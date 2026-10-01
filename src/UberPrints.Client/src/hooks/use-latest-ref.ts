import { useLayoutEffect, useRef } from 'react';

/**
 * Ref that always holds the latest value. Lets effects and callbacks read a
 * value without listing it as a dependency, so a change does not re-run them.
 */
export function useLatestRef<T>(value: T) {
  const ref = useRef(value);
  useLayoutEffect(() => {
    ref.current = value;
  });
  return ref;
}
