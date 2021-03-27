import { rm } from "fs/promises";

import release from "./release";
import readme from "./readme";
import esMain from "./es-main";
import { OUTPUT_PATH } from "./constants";

if (await esMain(import.meta.url)) {
  await rm(OUTPUT_PATH, {
    recursive: true,
    force: true,
    maxRetries: 3,
    retryDelay: 100,
  });

  await readme();
  await release();
}

export default {
  release,
  readme,
};
