import { readFile } from "fs/promises";
import path, { dirname } from "path";
import { fileURLToPath } from "url";
import memoize from "memoizee";
import Handlebars from "handlebars";

import { projectRelativePath } from "../utils";
import constants, { REPO_URL } from "../constants";
import { TemplateConfiguration } from "../configuration";

const __dirname = dirname(fileURLToPath(import.meta.moduleUrl));

function createHandlebars() {
  const esc = Handlebars.Utils.escapeExpression;
  const hbs = Handlebars.create();

  hbs.registerHelper(
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

  hbs.registerHelper(
    "sub",
    function sub(
      this: any,
      { hash: { text }, data }: Handlebars.HelperOptions,
    ) {
      if (data.root.release === "thunderstore") {
        return new Handlebars.SafeString(text);
      }

      return new Handlebars.SafeString(`<sub>${esc(text)}</sub>`);
    },
  );

  hbs.registerHelper(
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

  hbs.registerHelper(
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

  return hbs;
}

const readTemplate = async (filename: string) => {
  const tplPath = path.join(__dirname, filename);
  const data = await readFile(tplPath, "utf8");
  const relTplPath = projectRelativePath(tplPath);
  return `[//]: # (THIS FILE WAS AUTOMATICALLY GENERATED FROM ${relTplPath})\n\n${data}`;
};

export default async function generate(configuration: TemplateConfiguration) {
  const hbs = createHandlebars();

  const rawChangelog = await readTemplate("CHANGELOG.md");
  hbs.registerPartial("changelog", rawChangelog);

  const changelog = memoize(hbs.compile(rawChangelog));
  const readme = memoize(hbs.compile(await readTemplate("README.md")));

  const ctx = constants;

  return {
    readme: async () => readme({ ...ctx, release: configuration }),
    changelog: async () => changelog({ ...ctx, release: configuration }),
  };
}
