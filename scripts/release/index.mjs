#!/usr/bin/env node

import fs from "fs"
import { readFile, copyFile, rm, mkdir, stat } from "fs/promises"
import path, { dirname } from "path"
import { fileURLToPath } from "url"
import { promisify } from "util"
import child_process from "child_process"
import { Parser } from "xml2js"
import archiver from "archiver"
import esMain from "../es-main.mjs"
import readmeGenerator from "../readme/index.mjs"
import logger from "../logger.mjs"

const exec = promisify(child_process.exec)

import {
  PKG_NAME,
  PKG_AUTHOR,
  PKG_DESCRIPTION,
  PKG_LICENSE,
  REPO_URL,
  PKG_DEPENDENCIES,
} from "../constants.mjs"

const __dirname = dirname(fileURLToPath(import.meta.url))

const PLUGIN_DLL_NAME = `${PKG_NAME}.dll`
const CONFIG_RELEASE_STANDALONE = "Release-Standalone"
const CONFIG_RELEASE_THUNDERSTORE = "Release-Thunderstore"

const rootPath = path.resolve(path.join(__dirname, "../.."))
const pkgPath = path.join(rootPath, "pkg")
const thunderstorePkgPath = path.join(pkgPath, "thunderstore")
const standalonePkgPath = path.join(pkgPath, "standalone")
const pluginBinPath = path.join(rootPath, "QoLFix/bin")

function findCsprojProperty(csproj, name) {
  const prop = csproj.Project.PropertyGroup.find(x => x[name] != null)
  if (prop == null) return null
  return prop[name]
}

function getPropertyValue(csproj, name) {
  const prop = findCsprojProperty(csproj, name)

  if (prop == null) {
    throw new Error(`Missing MSBuild property: ${name}`)
  }

  if (prop.length > 1) {
    throw new Error(`${name} property was defined more than once`)
  }

  if (prop.length !== 1) {
    throw new Error(`Couldn't find ${name} property`)
  }

  return prop[0]
}

function createR2ModManManifest(version) {
  return {
    ManifestVersion: 2,
    AuthorName: PKG_AUTHOR,
    Name: `${PKG_AUTHOR}-${PKG_NAME}`,
    DisplayName: PKG_NAME,
    Version: version,
    License: PKG_LICENSE,
    WebsiteURL: REPO_URL,
    Description: PKG_DESCRIPTION,
    GameVersion: "N/A",
    Dependencies: PKG_DEPENDENCIES,
    OptionalDependencies: [],
    Incompatibilities: [],
    NetworkMode: "both",
    PackageType: "mod",
    InstallMode: "managed",
    Loaders: [ "bepinex" ],
    ExtraData: {},
  }
}

function createThunderstoreManifest(version) {
  return {
    name: PKG_NAME,
    description: PKG_DESCRIPTION,
    version_number: version,
    dependencies: PKG_DEPENDENCIES,
    website_url: REPO_URL,
  }
}

async function createThunderstorePackage(out, { pluginFile, manifest }) {
  const output = fs.createWriteStream(out)

  const archive = archiver("zip")

  archive.on("error", err => { throw err })
  archive.pipe(output)

  archive.append(Buffer.from(JSON.stringify(manifest, null, 2)), { name: "manifest.json" })
  archive.file(path.join(rootPath, "img/logo.png"), { name: "icon.png" })
  archive.file(path.join(thunderstorePkgPath, "README.md"), { name: "README.md" })
  archive.file(pluginFile, { name: "QoLFix.dll" })

  await archive.finalize()
}

async function main() {
  await rm(pkgPath, { recursive: true, force: true })

  const execOptions = { cwd: rootPath }

  logger.info("Generating README and CHANGELOG")
  await readmeGenerator()

  logger.info("Building")
  await Promise.all([
    exec(`dotnet build -c ${CONFIG_RELEASE_STANDALONE}`, execOptions),
    exec(`dotnet build -c ${CONFIG_RELEASE_THUNDERSTORE}`, execOptions),
  ])

  logger.info("Packaging")
  await mkdir(thunderstorePkgPath, { recursive: true })
  await mkdir(standalonePkgPath, { recursive: true })

  let data
  try {
    data = await readFile(path.join(__dirname, "../../QoLFix/QoLFix.csproj"), "utf8")
  }
  catch (e) {
    logger.error("Failed reading csproj", e)
    return
  }

  const parser = new Parser()
  const parseString = promisify(parser.parseString.bind(parser))

  let csproj
  try {
    csproj = await parseString(data)
  } catch (e) {
    logger.error("Failed parsing csproj", e)
    return
  }

  const targetFramework = getPropertyValue(csproj, "TargetFramework")

  const version = getPropertyValue(csproj, "Version")
  const prerelease = getPropertyValue(csproj, "VersionPrerelease")
  const semver = prerelease
    ? `${version}-${prerelease}`
    : version

  const thunderstorePackage = path.join(thunderstorePkgPath, `${PKG_NAME.toLowerCase()}-${semver}+thunderstore.zip`)
  const r2modmanPackage = path.join(thunderstorePkgPath, `${PKG_NAME.toLowerCase()}-${semver}+r2modman.zip`)
  const standaloneFile = path.join(standalonePkgPath, PLUGIN_DLL_NAME)

  const pluginFile = path.join(targetFramework, PLUGIN_DLL_NAME)
  const thunderstorePluginFile = path.join(pluginBinPath, CONFIG_RELEASE_THUNDERSTORE, pluginFile)
  const standalonePluginFile = path.join(pluginBinPath, CONFIG_RELEASE_STANDALONE, pluginFile)

  const gitProc = await exec("git diff --shortstat", execOptions)
  const isWorktreeDirty = !!gitProc.stdout

  if (!isWorktreeDirty && prerelease) {
    logger.warn("Thunderstore doesn't yet support SemVer; the Thunderstore package won't be created for this pre-release.")
  }

  if (isWorktreeDirty) {
    logger.warn("The git worktree is dirty. Only the r2modman package will be created.")
  }

  await Promise.all([
    (!prerelease && !isWorktreeDirty ? createThunderstorePackage(thunderstorePackage, {
      pluginFile: thunderstorePluginFile,
      manifest: createThunderstoreManifest(semver),
    }) : undefined),
    createThunderstorePackage(r2modmanPackage, {
      pluginFile: thunderstorePluginFile,
      manifest: createR2ModManManifest(semver),
    }),
    (!isWorktreeDirty ? copyFile(standalonePluginFile, standaloneFile) : undefined),
  ])

  logger.info("Done")
}

if (await esMain(import.meta)) {
  await main()
}

export default main
