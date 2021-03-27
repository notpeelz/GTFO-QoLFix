import { rm } from "fs/promises";

import release from "./release";
import readme from "./readme";
import esMain from "./es-main";
import { paths } from "./constants";

if (await esMain(import.meta.url)) {
  await rm(paths.OUTPUT, {
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
