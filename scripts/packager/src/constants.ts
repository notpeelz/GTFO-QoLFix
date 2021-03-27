import path from "path";
import { fileURLToPath } from "url";

const rootPath = path.resolve(
  fileURLToPath(import.meta.moduleUrl),
  "../../../..",
);

export const ROOT_PATH = rootPath;
export const PKG_NAME = "QoLFix";
export const PKG_AUTHOR = "notpeelz";
export const PKG_DESCRIPTION =
  "A general GTFO improvement mod that aims to fix various quality of life issues.";
export const PKG_LICENSE = "LGPL-3.0-or-later";
export const PKG_DEPENDENCIES = ["BepInEx-BepInExPack_GTFO-1.0.1"];
export const PKG_PATH = "notpeelz/QoLFix";
export const PKG_URL = `https://gtfo.thunderstore.io/package/${PKG_PATH}`;
export const REPO_PATH = "notpeelz/GTFO-QoLFix";
export const REPO_URL = `https://github.com/${REPO_PATH}`;
export default {
  ROOT_PATH,
  PKG_NAME,
  PKG_AUTHOR,
  PKG_DESCRIPTION,
  PKG_LICENSE,
  PKG_DEPENDENCIES,
  PKG_PATH,
  PKG_URL,
  REPO_PATH,
  REPO_URL,
};
