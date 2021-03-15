import { mkdirSync, readFileSync, writeFileSync } from "fs"
import path, { dirname } from "path"
import { fileURLToPath } from "url"
import Handlebars from "handlebars"

const __dirname = dirname(fileURLToPath(import.meta.url))
const REPO_URL = "https://github.com/notpeelz/GTFO-QoLFix"

Handlebars.registerHelper("ifEquals", (arg1, arg2, options) => {
  return (arg1 == arg2) ? options.fn(this) : options.inverse(this)
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

const changelog = readFileSync(path.join(__dirname, "CHANGELOG.md"), "utf8")
Handlebars.registerPartial("changelog", changelog)
const readme = Handlebars.compile(
  readFileSync(path.join(__dirname, "README.md"), "utf8")
)

const rootPath = "../.."
const thunderstorePkgPath = path.join(rootPath, "pkg/thunderstore")

mkdirSync(thunderstorePkgPath, { recursive: true })

// Thunderstore
writeFileSync(path.join(thunderstorePkgPath, "README.md"), readme({
  release: "thunderstore",
}))

// Standalone
writeFileSync(path.join(rootPath, "README.md"), readme({
  release: "standalone",
}))
writeFileSync(path.join(rootPath, "CHANGELOG.md"), changelog)
