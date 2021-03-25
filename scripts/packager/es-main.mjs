import { fileURLToPath } from "url"
import { lstat } from "fs/promises"
import process from "process"
import path from "path"
import logger from "./logger"

export default async function esMain(meta) {
  const modulePath = path.resolve(fileURLToPath(meta.url))
  const scriptPath = path.resolve(process.argv[1])

  let stats
  try {
    stats = await lstat(scriptPath)
  } catch (ex) {
    logger.error(`Failed calling stat() on ${scriptPath}`, ex)
  }

  if (stats) {
    const moduleBasename = path.basename(modulePath)
    const processScriptRanFromDir = stats.isDirectory()
    const isIndexModule = moduleBasename === "index.js" || moduleBasename === "index.mjs"

    if (processScriptRanFromDir && isIndexModule) {
      // Note: scriptPath is a directory here, so we don't want to call
      // dirname() or it would strip the directory name.
      return path.dirname(modulePath) === scriptPath
    }
  }

  return modulePath === scriptPath
}
