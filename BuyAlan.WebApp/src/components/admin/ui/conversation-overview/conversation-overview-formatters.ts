const MILLISECONDS_PER_DAY = 1000 * 60 * 60 * 24

const timeFormatter = new Intl.DateTimeFormat(undefined, {
  hour: "numeric",
  minute: "2-digit",
})

const weekdayFormatter = new Intl.DateTimeFormat(undefined, {
  weekday: "short",
})

const dateFormatter = new Intl.DateTimeFormat(undefined, {
  month: "short",
  day: "numeric",
})

const getDayDistance = (date: Date, reference: Date) => {
  const dateAtMidnight = new Date(date)
  dateAtMidnight.setHours(0, 0, 0, 0)

  const referenceAtMidnight = new Date(reference)
  referenceAtMidnight.setHours(0, 0, 0, 0)

  return Math.floor(
    (referenceAtMidnight.getTime() - dateAtMidnight.getTime()) / MILLISECONDS_PER_DAY,
  )
}

const parseTimestamp = (value: null | string) => {
  if (value === null || value.length === 0) {
    return null
  }

  const parsedValue = new Date(value)
  if (Number.isNaN(parsedValue.getTime())) {
    return null
  }

  return parsedValue
}

export const formatInboxTimestamp = (value: null | string) => {
  const timestamp = parseTimestamp(value)
  if (timestamp === null) {
    return ""
  }

  const now = new Date()
  const dayDistance = getDayDistance(timestamp, now)

  if (dayDistance <= 0) {
    return timeFormatter.format(timestamp)
  }

  if (dayDistance === 1) {
    return "Yesterday"
  }

  if (dayDistance < 7) {
    return weekdayFormatter.format(timestamp)
  }

  if (dayDistance < 14) {
    return "Last week"
  }

  const weeksAgo = Math.floor(dayDistance / 7)
  if (weeksAgo < 5) {
    return `${weeksAgo} weeks ago`
  }

  return dateFormatter.format(timestamp)
}

export const formatConversationDayLabel = (value: null | string) => {
  const timestamp = parseTimestamp(value)
  if (timestamp === null) {
    return "Conversation"
  }

  const now = new Date()
  const dayDistance = getDayDistance(timestamp, now)

  if (dayDistance <= 0) {
    return "Today"
  }

  if (dayDistance === 1) {
    return "Yesterday"
  }

  if (dayDistance < 7) {
    return weekdayFormatter.format(timestamp)
  }

  return dateFormatter.format(timestamp)
}
