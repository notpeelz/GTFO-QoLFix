/* eslint-disable no-console */
import colors from "colors";

function warn(...args: any[]) {
  return console.log(
    ...args.map((x) => (typeof x === "string" ? colors.yellow(x) : x)),
  );
}

function error(...args: any[]) {
  return console.log(
    ...args.map((x) => (typeof x === "string" ? colors.red(x) : x)),
  );
}

function info(...args: any[]) {
  return console.log(
    ...args.map((x) => (typeof x === "string" ? colors.grey(x) : x)),
  );
}

function log(...args: any[]) {
  return console.log(...args);
}

export default {
  warn,
  error,
  log,
  info,
};
