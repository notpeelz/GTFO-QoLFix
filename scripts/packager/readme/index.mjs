#!/usr/bin/env -S node --es-module-specifier-resolution=node
// NOTE: for some reason es-module-specifier-resolution is necessary
// otherwise importing modules without specfiying the ".mjs" extension fails.

import fs from "fs"
import path, { dirname } from "path"
import { fileURLToPath } from "url"
import { promisify } from "util"
import Handlebars from "handlebars"
import esMain from "../es-main"

import constants, {
  ROOT_PATH,
  REPO_URL,
} from "../constants"

const mkdir = promisify(fs.mkdir)
const readFile = promisify(fs.readFile)
const writeFile = promisify(fs.writeFile)
const esc = Handlebars.Utils.escapeExpression

const __dirname = dirname(fileURLToPath(import.meta.url))

const pkgPath = path.join(ROOT_PATH, "pkg")
const thunderstorePkgPath = path.join(pkgPath, "thunderstore")
const standalonePkgPath = path.join(pkgPath, "standalone")

async function main() {
  Handlebars.registerHelper("ifEquals", (arg1, arg2, options) => {
    return (arg1 === arg2) ? options.fn(this) : options.inverse(this)
  })

  Handlebars.registerHelper("sub", ({ hash: { text }, data }) => {
    if (data.root.release === "thunderstore") return new Handlebars.SafeString(text)
    return new Handlebars.SafeString(`<sub>${esc(text)}</sub>`)
  })

  Handlebars.registerHelper("embedVideo", ({ hash, data }) => {
    const { name, ext = "jpg", height, url } = hash
    const isThunderstore = data.root.release === "thunderstore"

    let imgPath = `img/${name}_thumbnail.${ext}`
    if (isThunderstore) {
      imgPath = `${REPO_URL}/raw/master/${imgPath}`
    }

    // Thunderstore's Markdown flavor doesn't support HTML tags :/
    if (height != null && !isThunderstore) {
      return new Handlebars.SafeString(
        `<a href="${esc(url)}"><img height="${esc(height)}" src="${esc(imgPath)}"></a>`,
      )
    }

    return `[![${name}](${imgPath})](${url})`
  })

  Handlebars.registerHelper("embedImage", ({ hash, data }) => {
    const { name, ext = "jpg", height } = hash
    const isThunderstore = data.root.release === "thunderstore"

    let imgPath = `img/${name}.${ext}`
    if (data.root.release === "thunderstore") {
      imgPath = `${REPO_URL}/raw/master/${imgPath}`
    }

    if (height != null && !isThunderstore) {
      return new Handlebars.SafeString(
        `<img height="${esc(height)}" src="${esc(imgPath)}">`,
      )
    }

    return `![${name}](${imgPath})`
  })

  const readTemplate = async (filename) => {
    const tplPath = path.join(__dirname, filename)
    const data = await readFile(tplPath, "utf8")
    const relTplPath = path.relative(ROOT_PATH, tplPath).replace(new RegExp("\\\\", "g"), "/")
    return `[//]: # (THIS FILE WAS AUTOMATICALLY GENERATED FROM ${relTplPath})\n\n${data}`
  }

  const rawChangelog = await readTemplate("CHANGELOG.md")
  Handlebars.registerPartial("changelog", rawChangelog)
  const changelog = Handlebars.compile(rawChangelog)

  const readme = Handlebars.compile(await readTemplate("README.md"))

  await mkdir(thunderstorePkgPath, { recursive: true })
  await mkdir(standalonePkgPath, { recursive: true })

  const ctx = constants

  // Thunderstore
  await writeFile(path.join(thunderstorePkgPath, "README.md"), readme({ ...ctx, release: "thunderstore" }))

  // Standalone
  await writeFile(path.join(standalonePkgPath, "README.md"), readme({ ...ctx, release: "standalone" }))
  await writeFile(path.join(standalonePkgPath, "CHANGELOG.md"), changelog({ ...ctx, release: "standalone" }))

  // Repo
  await writeFile(path.join(ROOT_PATH, "README.md"), readme({ ...ctx, release: "standalone" }))
  await writeFile(path.join(ROOT_PATH, "CHANGELOG.md"), changelog({ ...ctx, release: "standalone" }))
}

if (await esMain(import.meta)) {
  await main()
}

export default main
