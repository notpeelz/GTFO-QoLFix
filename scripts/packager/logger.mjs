/* eslint-disable no-console */
import colors from "colors"

function warn(...args) {
  return console.log(...args.map((x) => typeof(x) === "string" ? colors.yellow(x) : x))
}

function error(...args) {
  return console.log(...args.map((x) => typeof(x) === "string" ? colors.red(x) : x))
}

function info(...args) {
  return console.log(...args.map((x) => typeof(x) === "string" ? colors.grey(x) : x))
}

function log(...args) {
  return console.log(...args)
}

export default {
  warn,
  error,
  log,
  info,
}
