import path from "path";
import { paths } from "./constants";

export function projectRelativePath(absPath: string) {
  const relPath = path.relative(paths.ROOT, absPath);

  if (process.platform === "win32") {
    return relPath.replace(new RegExp("\\\\", "g"), "/");
  }

  return relPath;
}

export default {
  projectRelativePath,
};
