import colors from 'colors'

export function warn(...args) {
  return console.log(...args.map(x => typeof(x) === 'string' ? colors.yellow(x) : x))
}

export function error(...args) {
  return console.log(...args.map(x => typeof(x) === 'string' ? colors.red(x) : x))
}

export function info(...args) {
  return console.log(...args.map(x => typeof(x) === 'string' ? colors.grey(x) : x))
}

export function log(...args) {
  return console.log(...args)
}

export default {
  warn,
  error,
  log,
  info,
}
