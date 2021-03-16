#!/usr/bin/env node

import fs from "fs"
import path, { dirname } from "path"
import { fileURLToPath } from "url"
import { promisify } from "util"
import Handlebars from "handlebars"
import esMain from "../es-main.mjs"

const mkdir = promisify(fs.mkdir)
const readFile = promisify(fs.readFile)
const writeFile = promisify(fs.writeFile)

import { REPO_URL, REPO_PATH } from "../constants.mjs"

const __dirname = dirname(fileURLToPath(import.meta.url))

const rootPath = path.resolve(path.join(__dirname, "../.."))
const pkgPath = path.join(rootPath, "pkg")
const thunderstorePkgPath = path.join(pkgPath, "thunderstore")
const standalonePkgPath = path.join(pkgPath, "standalone")

async function main() {
  Handlebars.registerHelper("ifEquals", (arg1, arg2, options) => {
    return (arg1 == arg2) ? options.fn(this) : options.inverse(this)
  })

  Handlebars.registerHelper("sub", ({ hash: { text }, data }) => {
    if (data.root.release === "thunderstore") return new Handlebars.SafeString(text)
    return new Handlebars.SafeString("<sub>" + Handlebars.Utils.escapeExpression(text) + "</sub>")
  })

  Handlebars.registerHelper("embedVideo", ({ hash, data }) => {
    const { name, ext = "jpg", url } = hash
    let imgPath = `img/${name}_thumbnail.${ext}`
    if (data.root.release === "thunderstore") {
      imgPath = `${REPO_URL}/raw/master/${imgPath}`
    }
    return `[![${name}](${imgPath})](${url})`
  })

  Handlebars.registerHelper("embedImage", ({ hash: { name, ext = "jpg" }, data }) => {
    let imgPath = `img/${name}.${ext}`
    if (data.root.release === "thunderstore") {
      imgPath = `${REPO_URL}/raw/master/${imgPath}`
    }
    return `![${name}](${imgPath})`
  })

  const readTemplate = async (filename) => {
    const tplPath = path.join(__dirname, filename)
    const data = await readFile(tplPath, "utf8")
    const relTplPath = path.relative(rootPath, tplPath).replace(new RegExp('\\\\', 'g'), '/')
    return `[//]: # (THIS FILE WAS AUTOMATICALLY GENERATED FROM ${relTplPath})\n\n${data}`
  }

  const rawChangelog = await readTemplate("CHANGELOG.md")
  Handlebars.registerPartial("changelog", rawChangelog)
  const changelog = Handlebars.compile(rawChangelog)

  const readme = Handlebars.compile(await readTemplate("README.md"))

  await mkdir(thunderstorePkgPath, { recursive: true })
  await mkdir(standalonePkgPath, { recursive: true })

  const ctx = {
    REPO_PATH,
    REPO_URL,
  }

  // Thunderstore
  await writeFile(path.join(thunderstorePkgPath, "README.md"), readme({ ...ctx, release: "thunderstore" }))

  // Standalone
  await writeFile(path.join(standalonePkgPath, "README.md"), readme({ ...ctx, release: "standalone" }))
  await writeFile(path.join(standalonePkgPath, "CHANGELOG.md"), changelog({ ...ctx, release: "standalone" }))

  // Repo
  await writeFile(path.join(rootPath, "README.md"), readme({ ...ctx, release: "standalone" }))
  await writeFile(path.join(rootPath, "CHANGELOG.md"), changelog({ ...ctx, release: "standalone" }))
}

if (await esMain(import.meta)) {
  await main()
}

export default main
