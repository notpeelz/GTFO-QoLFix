import fs from "fs";
import path, { dirname } from "path";
import { fileURLToPath } from "url";
import { promisify } from "util";
import Handlebars from "handlebars";

import logger from "../logger";
import { projectRelativePath } from "../utils";
import constants, { paths, REPO_URL } from "../constants";

const mkdir = promisify(fs.mkdir);
const readFile = promisify(fs.readFile);
const writeFile = promisify(fs.writeFile);
const esc = Handlebars.Utils.escapeExpression;

const __dirname = dirname(fileURLToPath(import.meta.moduleUrl));

Handlebars.registerHelper(
  "ifEquals",
  function ifEquals(
    this: any,
    arg1: string,
    arg2: string,
    options: Handlebars.HelperOptions,
  ) {
    return arg1 === arg2 ? options.fn(this) : options.inverse(this);
  },
);

Handlebars.registerHelper(
  "sub",
  function sub(this: any, { hash: { text }, data }: Handlebars.HelperOptions) {
    if (data.root.release === "thunderstore") {
      return new Handlebars.SafeString(text);
    }

    return new Handlebars.SafeString(`<sub>${esc(text)}</sub>`);
  },
);

Handlebars.registerHelper(
  "embedVideo",
  function embedVideo(this: any, { hash, data }: Handlebars.HelperOptions) {
    const { name, ext = "jpg", height, url } = hash;
    const isThunderstore = data.root.release === "thunderstore";

    let imgPath = `img/${name}_thumbnail.${ext}`;
    if (isThunderstore) {
      imgPath = `${REPO_URL}/raw/master/${imgPath}`;
    }

    // Thunderstore's Markdown flavor doesn't support HTML tags :/
    if (height != null && !isThunderstore) {
      return new Handlebars.SafeString(
        `<a href="${esc(url)}"><img height="${esc(height)}" src="${esc(
          imgPath,
        )}"></a>`,
      );
    }

    return `[![${name}](${imgPath})](${url})`;
  },
);

Handlebars.registerHelper(
  "embedImage",
  function embedImage(this: any, { hash, data }: Handlebars.HelperOptions) {
    const { name, ext = "jpg", height } = hash;
    const isThunderstore = data.root.release === "thunderstore";

    let imgPath = `img/${name}.${ext}`;
    if (data.root.release === "thunderstore") {
      imgPath = `${REPO_URL}/raw/master/${imgPath}`;
    }

    if (height != null && !isThunderstore) {
      return new Handlebars.SafeString(
        `<img height="${esc(height)}" src="${esc(imgPath)}">`,
      );
    }

    return `![${name}](${imgPath})`;
  },
);

export default async function main() {
  logger.info("Generating README and CHANGELOG");

  const readTemplate = async (filename: string) => {
    const tplPath = path.join(__dirname, filename);
    const data = await readFile(tplPath, "utf8");
    const relTplPath = projectRelativePath(tplPath);
    return `[//]: # (THIS FILE WAS AUTOMATICALLY GENERATED FROM ${relTplPath})\n\n${data}`;
  };

  const rawChangelog = await readTemplate("CHANGELOG.md");
  Handlebars.registerPartial("changelog", rawChangelog);
  const changelog = Handlebars.compile(rawChangelog);

  const readme = Handlebars.compile(await readTemplate("README.md"));

  await mkdir(paths.OUTPUT_THUNDERSTORE, { recursive: true });
  await mkdir(paths.OUTPUT_STANDALONE, { recursive: true });

  const ctx = constants;

  // Thunderstore
  await writeFile(
    path.join(paths.OUTPUT_THUNDERSTORE, "README.md"),
    readme({ ...ctx, release: "thunderstore" }),
  );

  // Standalone
  await writeFile(
    path.join(paths.OUTPUT_STANDALONE, "README.md"),
    readme({ ...ctx, release: "standalone" }),
  );
  await writeFile(
    path.join(paths.OUTPUT_STANDALONE, "CHANGELOG.md"),
    changelog({ ...ctx, release: "standalone" }),
  );

  // Repo
  await writeFile(
    path.join(paths.ROOT, "README.md"),
    readme({ ...ctx, release: "standalone" }),
  );
  await writeFile(
    path.join(paths.ROOT, "CHANGELOG.md"),
    changelog({ ...ctx, release: "standalone" }),
  );
}
