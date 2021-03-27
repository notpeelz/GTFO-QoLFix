import path, { dirname } from "path";
import { fileURLToPath, pathToFileURL } from "url";
import typescript from "@rollup/plugin-typescript";
import json from "@rollup/plugin-json";

const __dirname = dirname(fileURLToPath(import.meta.url));

function importMetaUrl() {
  return {
    name: "resolveMetaUrl",
    resolveImportMeta: (property, chunk) => {
      if (property === "moduleUrl") {
        const moduleUrl = pathToFileURL(chunk.moduleId);
        return JSON.stringify(moduleUrl.toString());
      }
    },
  }
}

export default {
  input: path.resolve(__dirname, "src/index.ts"),
  output: {
    file: path.resolve(__dirname, "dist/index.mjs"),
    format: "es",
  },
  plugins: [
    typescript(),
    importMetaUrl(),
    json(),
  ],
}
