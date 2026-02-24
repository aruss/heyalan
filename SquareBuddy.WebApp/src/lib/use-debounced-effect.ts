import { useEffect } from 'react';

/**
 * Defines the props for the useDebouncedEffect hook.
 * TData is a generic type representing the data to be fetched.
 */
interface UseDebouncedEffectProps<TData> {
  /**
   * The memoized async function to call.
   * It MUST accept an AbortSignal as its only argument
   * and return a Promise resolving to TData.
   */
  effectCallback: (signal: AbortSignal) => Promise<TData>;

  /**
   * The React state setter function to call with the resolved data.
   */
  setData: React.Dispatch<React.SetStateAction<TData>>;

  /**
   * (Optional) The React state setter for handling errors.
   */
  setError?: React.Dispatch<React.SetStateAction<Error | null>> | null;

  /**
   * (Optional) The debounce delay in milliseconds.
   * @default 300
   */
  delay?: number;
}

/**
 * Executes a debounced, abortable effect callback when its reference changes.
 *
 * @param {UseDebouncedEffectProps<TData>} props - The hook's configuration object.
 */
export const useDebouncedEffect = <TData>({
  effectCallback,
  setData,
  setError = null,
  delay = 300,
}: UseDebouncedEffectProps<TData>) => {
  useEffect(() => {
    // Create an AbortController for this effect run
    const controller = new AbortController();

    // Set up the debounced timer
    const timerId = setTimeout(() => {
      const runEffect = async () => {
        try {
          // Call the user's memoized callback, passing only the signal
          // The 'data' variable will be correctly inferred as type TData
          const data = await effectCallback(controller.signal);

          // Set data and clear any previous error
          setData(data);
          if (setError) setError(null);

        } catch (error) {
          // Handle and type-check errors
          if (error instanceof Error) {
            if (error.name !== 'AbortError') {
              console.error("Effect error:", error);
              if (setError) {
                setError(error);
              }
            }
          } else {
            // Handle non-Error throwables
            console.error("An unknown error occurred:", error);
            if (setError) {
              setError(new Error(String(error)));
            }
          }
        }
      };

      runEffect();
    }, delay);

    // The cleanup function
    return () => {
      clearTimeout(timerId); // Clear the timeout if deps change early
      controller.abort();    // Abort the effect/fetch
    };

    // The effect runs whenever the callback reference changes (due to
    // its own dependencies in useCallback) or if the setters/delay change.
  }, [effectCallback, setData, setError, delay]);
};