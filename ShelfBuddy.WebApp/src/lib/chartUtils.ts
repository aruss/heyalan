// Tremor Custom chartColors

export type ColorUtility = "bg" | "stroke" | "fill" | "text"

export const chartColors = {
  blue: {
    bg: "bg-gray-500 dark:bg-gray-500",
    stroke: "stroke-gray-500 dark:stroke-gray-500",
    fill: "fill-gray-500 dark:fill-gray-500",
    text: "text-gray-500 dark:text-gray-500",
  },
  lightBlue: {
    bg: "bg-gray-300/50 dark:bg-gray-800/50",
    stroke: "stroke-gray-300/50 dark:stroke-gray-800/50",
    fill: "fill-gray-300/50 dark:fill-gray-800/50",
    text: "text-gray-300/50 dark:text-gray-800/50",
  },
  emerald: {
    bg: "bg-gray-500 dark:bg-gray-500",
    stroke: "stroke-gray-500 dark:stroke-gray-500",
    fill: "fill-gray-500 dark:fill-gray-500",
    text: "text-gray-500 dark:text-gray-500",
  },
  lightEmerald: {
    bg: "bg-gray-300/50 dark:bg-gray-800/50",
    stroke: "stroke-gray-300/50 dark:stroke-gray-800/50",
    fill: "fill-gray-300/50 dark:fill-gray-800/50",
    text: "text-gray-300/50 dark:text-gray-800/50",
  },
  violet: {
    bg: "bg-gray-500 dark:bg-gray-500",
    stroke: "stroke-gray-500 dark:stroke-gray-500",
    fill: "fill-gray-500 dark:fill-gray-500",
    text: "text-gray-500 dark:text-gray-500",
  },
  amber: {
    bg: "bg-gray-500 dark:bg-gray-500",
    stroke: "stroke-gray-500 dark:stroke-gray-500",
    fill: "fill-gray-500 dark:fill-gray-500",
    text: "text-gray-500 dark:text-gray-500",
  },
  gray: {
    bg: "bg-gray-400 dark:bg-gray-600",
    stroke: "stroke-gray-400 dark:stroke-gray-600",
    fill: "fill-gray-400 dark:fill-gray-600",
    text: "text-gray-400 dark:text-gray-600",
  },
  rose: {
    bg: "bg-gray-600 dark:bg-gray-500",
    stroke: "stroke-gray-600 dark:stroke-gray-500",
    fill: "fill-gray-600 dark:fill-gray-500",
    text: "text-gray-600 dark:text-gray-500",
  },
  sky: {
    bg: "bg-gray-500 dark:bg-gray-500",
    stroke: "stroke-gray-500 dark:stroke-gray-500",
    fill: "fill-gray-500 dark:fill-gray-500",
    text: "text-gray-500 dark:text-gray-500",
  },
  cyan: {
    bg: "bg-gray-500 dark:bg-gray-500",
    stroke: "stroke-gray-500 dark:stroke-gray-500",
    fill: "fill-gray-500 dark:fill-gray-500",
    text: "text-gray-500 dark:text-gray-500",
  },
  indigo: {
    bg: "bg-gray-600 dark:bg-gray-500",
    stroke: "stroke-gray-600 dark:stroke-gray-500",
    fill: "fill-gray-600 dark:fill-gray-500",
    text: "text-gray-600 dark:text-gray-500",
  },
  orange: {
    bg: "bg-gray-500 dark:bg-gray-400",
    stroke: "stroke-gray-500 dark:stroke-gray-400",
    fill: "fill-gray-500 dark:fill-gray-400",
    text: "text-gray-500 dark:text-gray-400",
  },
  pink: {
    bg: "bg-gray-500 dark:bg-gray-500",
    stroke: "stroke-gray-500 dark:stroke-gray-500",
    fill: "fill-gray-500 dark:fill-gray-500",
    text: "text-gray-500 dark:text-gray-500",
  },
  red: {
    bg: "bg-gray-500 dark:bg-gray-500",
    stroke: "stroke-gray-500 dark:stroke-gray-500",
    fill: "fill-gray-500 dark:fill-gray-500",
    text: "text-gray-500 dark:text-gray-500",
  },
  lightGray: {
    bg: "bg-gray-300 dark:bg-gray-700",
    stroke: "stroke-gray-300 dark:stroke-gray-700",
    fill: "fill-gray-300 dark:fill-gray-700",
    text: "text-gray-300 dark:text-gray-700",
  },
} as const satisfies {
  [color: string]: {
    [key in ColorUtility]: string
  }
}

export type AvailableChartColorsKeys = keyof typeof chartColors

export const chartGradientColors = {
  blue: "from-gray-200 to-gray-500 dark:from-gray-200/10 dark:to-gray-400",
  lightBlue: "from-gray-200 to-gray-500 dark:from-gray-200/10 dark:to-gray-400",
  emerald:
    "from-gray-200 to-gray-500 dark:from-gray-200/10 dark:to-gray-400",
  lightEmerald:
    "from-gray-200 to-gray-500 dark:from-gray-200/10 dark:to-gray-400",
  violet:
    "from-gray-200 to-gray-500 dark:from-gray-200/10 dark:to-gray-400",
  amber: "from-gray-200 to-gray-500 dark:from-gray-200/10 dark:to-gray-400",
  gray: "from-gray-200 to-gray-500 dark:from-gray-200/10 dark:to-gray-400",
  lightGray: "from-gray-200 to-gray-500 dark:from-gray-200/10 dark:to-gray-400",
  rose: "from-gray-200 to-gray-500 dark:from-gray-200/10 dark:to-gray-400",
  sky: "from-gray-200 to-gray-500 dark:from-gray-200/10 dark:to-gray-400",
  cyan: "from-gray-200 to-gray-500 dark:from-gray-200/10 dark:to-gray-400",
  indigo:
    "from-gray-200 to-gray-500 dark:from-gray-200/10 dark:to-gray-400",
  orange:
    "from-gray-200 to-gray-500 dark:from-gray-200/10 dark:to-gray-400",
  pink: "from-gray-200 to-gray-500 dark:from-gray-200/10 dark:to-gray-400",
  red: "from-gray-200 to-gray-500 dark:from-gray-200/10 dark:to-gray-400",
} as const satisfies Record<string, string>

export const chartConditionalColors = {
  blue: {
    low: "fill-gray-200 dark:fill-gray-300",
    medium: "fill-gray-300 dark:fill-gray-400",
    high: "fill-gray-400 dark:fill-gray-500",
    critical: "fill-gray-500 dark:fill-gray-600",
  },
  lightBlue: {
    low: "fill-gray-200 dark:fill-gray-300",
    medium: "fill-gray-300 dark:fill-gray-400",
    high: "fill-gray-400 dark:fill-gray-500",
    critical: "fill-gray-500 dark:fill-gray-600",
  },
  emerald: {
    low: "fill-gray-200 dark:fill-gray-300",
    medium: "fill-gray-300 dark:fill-gray-400",
    high: "fill-gray-400 dark:fill-gray-500",
    critical: "fill-gray-500 dark:fill-gray-600",
  },
  lightEmerald: {
    low: "fill-gray-200 dark:fill-gray-300",
    medium: "fill-gray-300 dark:fill-gray-400",
    high: "fill-gray-400 dark:fill-gray-500",
    critical: "fill-gray-500 dark:fill-gray-600",
  },
  violet: {
    low: "fill-gray-200 dark:fill-gray-300",
    medium: "fill-gray-300 dark:fill-gray-400",
    high: "fill-gray-400 dark:fill-gray-500",
    critical: "fill-gray-500 dark:fill-gray-600",
  },
  amber: {
    low: "fill-gray-200 dark:fill-gray-300",
    medium: "fill-gray-300 dark:fill-gray-400",
    high: "fill-gray-400 dark:fill-gray-500",
    critical: "fill-gray-500 dark:fill-gray-600",
  },
  gray: {
    low: "fill-gray-200 dark:fill-gray-300",
    medium: "fill-gray-300 dark:fill-gray-400",
    high: "fill-gray-400 dark:fill-gray-500",
    critical: "fill-gray-500 dark:fill-gray-600",
  },
  rose: {
    low: "fill-gray-200 dark:fill-gray-300",
    medium: "fill-gray-300 dark:fill-gray-400",
    high: "fill-gray-400 dark:fill-gray-500",
    critical: "fill-gray-500 dark:fill-gray-600",
  },
  sky: {
    low: "fill-gray-200 dark:fill-gray-300",
    medium: "fill-gray-300 dark:fill-gray-400",
    high: "fill-gray-400 dark:fill-gray-500",
    critical: "fill-gray-500 dark:fill-gray-600",
  },
  cyan: {
    low: "fill-gray-200 dark:fill-gray-300",
    medium: "fill-gray-300 dark:fill-gray-400",
    high: "fill-gray-400 dark:fill-gray-500",
    critical: "fill-gray-500 dark:fill-gray-600",
  },
  indigo: {
    low: "fill-gray-200 dark:fill-gray-300",
    medium: "fill-gray-300 dark:fill-gray-400",
    high: "fill-gray-400 dark:fill-gray-500",
    critical: "fill-gray-500 dark:fill-gray-600",
  },
  orange: {
    low: "fill-gray-200 dark:fill-gray-300",
    medium: "fill-gray-300 dark:fill-gray-400",
    high: "fill-gray-400 dark:fill-gray-500",
    critical: "fill-gray-500 dark:fill-gray-600",
  },
  pink: {
    low: "fill-gray-200 dark:fill-gray-300",
    medium: "fill-gray-300 dark:fill-gray-400",
    high: "fill-gray-400 dark:fill-gray-500",
    critical: "fill-gray-500 dark:fill-gray-600",
  },
  red: {
    low: "fill-gray-200 dark:fill-gray-300",
    medium: "fill-gray-300 dark:fill-gray-400",
    high: "fill-gray-400 dark:fill-gray-500",
    critical: "fill-gray-500 dark:fill-gray-600",
  },
  lightGray: {
    low: "fill-gray-200 dark:fill-gray-300",
    medium: "fill-gray-300 dark:fill-gray-400",
    high: "fill-gray-400 dark:fill-gray-500",
    critical: "fill-gray-500 dark:fill-gray-600",
  },
}

export type AvailableChartConditionalColorsKeys = keyof typeof chartColors

export const AvailableChartColors: AvailableChartColorsKeys[] = Object.keys(
  chartColors,
) as Array<AvailableChartColorsKeys>

export const constructCategoryColors = (
  categories: string[],
  colors: AvailableChartColorsKeys[],
): Map<string, AvailableChartColorsKeys> => {
  const categoryColors = new Map<string, AvailableChartColorsKeys>()
  categories.forEach((category, index) => {
    categoryColors.set(category, colors[index % colors.length])
  })
  return categoryColors
}

export const getColorClassName = (
  color: AvailableChartColorsKeys,
  type: ColorUtility,
): string => {
  const fallbackColor = {
    bg: "bg-gray-500",
    stroke: "stroke-gray-500",
    fill: "fill-gray-500",
    text: "text-gray-500",
  }
  return chartColors[color]?.[type] ?? fallbackColor[type]
}

export const getGradientColorClassName = (
  color: AvailableChartColorsKeys,
): string => {
  return chartGradientColors[color]
}

export const getConditionalColorClassName = (
  value: number,
  color: AvailableChartConditionalColorsKeys,
) => {
  const fallbackColors = {
    low: "fill-gray-300 dark:fill-gray-400",
    medium: "fill-gray-400 dark:fill-gray-500",
    high: "fill-gray-500 dark:fill-gray-600",
    critical: "fill-gray-600 dark:fill-gray-700",
  }

  const classes = chartConditionalColors[color] ?? fallbackColors

  if (value <= 0.25) return classes.low
  if (value <= 0.5) return classes.medium
  if (value <= 0.75) return classes.high
  return classes.critical
}

// Tremor Raw getYAxisDomain [v0.0.0]

export const getYAxisDomain = (
  autoMinValue: boolean,
  minValue: number | undefined,
  maxValue: number | undefined,
) => {
  const minDomain = autoMinValue ? "auto" : (minValue ?? 0)
  const maxDomain = maxValue ?? "auto"
  return [minDomain, maxDomain]
}

// Tremor Raw hasOnlyOneValueForKey [v0.1.0]

export function hasOnlyOneValueForKey(
  array: any[],
  keyToCheck: string,
): boolean {
  const val: any[] = []

  for (const obj of array) {
    if (Object.prototype.hasOwnProperty.call(obj, keyToCheck)) {
      val.push(obj[keyToCheck])
      if (val.length > 1) {
        return false
      }
    }
  }

  return true
}
