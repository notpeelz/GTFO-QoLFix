import { fileURLToPath } from "url";
import { lstat } from "fs/promises";
import process from "process";
import path from "path";

import logger from "../logger";
import pkgJson from "../../package.json";

async function checkScript(moduleUrl: string, scriptPath: string) {
  const modulePath = path.resolve(fileURLToPath(moduleUrl));

  let stats;
  try {
    stats = await lstat(scriptPath);
  } catch (ex) {
    logger.error(`Failed calling stat() on ${scriptPath}`, ex);
  }

  if (stats) {
    const modulePathComponents = path.parse(modulePath);
    const processScriptRanFromDir = stats.isDirectory();
    const isIndexModule = modulePathComponents.name === "index";

    if (processScriptRanFromDir && isIndexModule) {
      // Note: scriptPath is a directory here, so we don't want to call
      // dirname() or it would strip the directory name.
      return path.dirname(modulePath) === scriptPath;
    }
  }

  return modulePath === scriptPath;
}

export default async function esMain(moduleUrl: string) {
  return (
    checkScript(moduleUrl, path.resolve(pkgJson.main)) ||
    checkScript(moduleUrl, path.resolve(process.argv[1]))
  );
}
