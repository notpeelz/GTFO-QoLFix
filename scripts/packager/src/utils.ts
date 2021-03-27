import path from "path";
import { ROOT_PATH } from "./constants";

export function projectRelativePath(absPath: string) {
  const relPath = path.relative(ROOT_PATH, absPath);

  if (process.platform === "win32") {
    return relPath.replace(new RegExp("\\\\", "g"), "/");
  }

  return relPath;
}

export default {
  projectRelativePath,
};
