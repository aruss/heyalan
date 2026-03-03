import * as React from "react"

const MOBILE_BREAKPOINT = 768

export interface MobileState {
  isMobile: boolean
  isResolved: boolean
}

export function useMobileState(): MobileState {
  const [isMobile, setIsMobile] = React.useState<boolean | undefined>(undefined)

  React.useEffect(() => {
    const mediaQueryList = window.matchMedia(`(max-width: ${MOBILE_BREAKPOINT - 1}px)`)
    const onChange = () => {
      setIsMobile(window.innerWidth < MOBILE_BREAKPOINT)
    }

    mediaQueryList.addEventListener("change", onChange)
    onChange()

    return () => {
      mediaQueryList.removeEventListener("change", onChange)
    }
  }, [])

  const isResolved = isMobile !== undefined
  return {
    isMobile: !!isMobile,
    isResolved,
  }
}

export function useIsMobile() {
  const mobileState = useMobileState()
  return mobileState.isMobile
}
