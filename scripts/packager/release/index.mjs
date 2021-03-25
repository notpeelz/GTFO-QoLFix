#!/usr/bin/env -S node --es-module-specifier-resolution=node
// NOTE: for some reason es-module-specifier-resolution is necessary
// otherwise importing modules without specfiying the ".mjs" extension fails.

import fs from "fs"
import { readFile, copyFile, rm, mkdir } from "fs/promises"
import path from "path"
import { promisify } from "util"
import childProcess from "child_process"
import { Parser } from "xml2js"
import archiver from "archiver"
import esMain from "../es-main"
import readmeGenerator from "../readme"
import logger from "../logger"

import {
  ROOT_PATH,
  PKG_NAME,
  PKG_AUTHOR,
  PKG_DESCRIPTION,
  PKG_LICENSE,
  REPO_URL,
  PKG_DEPENDENCIES,
} from "../constants"

const exec = promisify(childProcess.exec)

const PLUGIN_DLL_NAME = `${PKG_NAME}.dll`
const CONFIG_RELEASE_STANDALONE = "Release-Standalone"
const CONFIG_RELEASE_THUNDERSTORE = "Release-Thunderstore"
const CONFIG_DEBUG = "Debug"

const pkgPath = path.join(ROOT_PATH, "pkg")
const thunderstorePkgPath = path.join(pkgPath, "thunderstore")
const standalonePkgPath = path.join(pkgPath, "standalone")
const pluginBinPath = path.join(ROOT_PATH, "QoLFix/bin")

const execOptions = { cwd: ROOT_PATH }

function findCsprojProperty(csproj, name) {
  const prop = csproj.Project.PropertyGroup.find((x) => x[name] != null)
  if (prop == null) return null
  return prop[name]
}

function getPropertyValue(csproj, name) {
  const prop = findCsprojProperty(csproj, name)

  if (prop == null || prop.length === 0) {
    throw new Error(`Missing MSBuild property: ${name}`)
  }

  if (prop.length > 1) {
    throw new Error(`MSBuild property was defined more than once: ${name}`)
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
    Loaders: ["bepinex"],
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

  archive.on("error", (err) => { throw err })
  archive.pipe(output)

  archive.append(Buffer.from(JSON.stringify(manifest, null, 2)), { name: "manifest.json" })
  archive.file(path.join(ROOT_PATH, "img/logo.png"), { name: "icon.png" })
  archive.file(path.join(thunderstorePkgPath, "README.md"), { name: "README.md" })
  archive.file(pluginFile, { name: "QoLFix.dll" })

  await archive.finalize()
}

async function getVersionInfo(version, prerelease) {
  const r = new RegExp("\n", "g")
  const getStdout = (x) => x.stdout.replace(r, "")

  let semver = prerelease
    ? `${version}-${prerelease}`
    : version

  const gitHashPromise = exec("git describe --long --always --dirty --exclude=* --abbrev=7", execOptions)
  const gitHash = await gitHashPromise
  if (gitHashPromise.child.exitCode !== 0) {
    throw new Error(`Failed to get git commit hash: exit code ${gitHashPromise.child.exitCode}`)
  }

  const gitBranchPromise = exec("git branch --show-current", execOptions)
  const gitBranch = await gitBranchPromise
  if (gitBranchPromise.child.exitCode !== 0) {
    throw new Error(`Failed to get git branch: exit code ${gitBranchPromise.child.exitCode}`)
  }

  semver += `+git${getStdout(gitHash)}-${getStdout(gitBranch)}`

  return {
    semver,
    commit: gitHash,
    branch: gitBranch,
  }
}

async function main() {
  await rm(pkgPath, {
    recursive: true,
    force: true,
    maxRetries: 3,
    retryDelay: 100,
  })

  logger.info("Generating README and CHANGELOG")
  await readmeGenerator()

  logger.info("Building")
  await Promise.all([
    exec(`dotnet build -c ${CONFIG_DEBUG}`, execOptions),
    exec(`dotnet build -c ${CONFIG_RELEASE_STANDALONE}`, execOptions),
    exec(`dotnet build -c ${CONFIG_RELEASE_THUNDERSTORE}`, execOptions),
  ])

  logger.info("Packaging")
  await mkdir(thunderstorePkgPath, { recursive: true })
  await mkdir(standalonePkgPath, { recursive: true })

  let data
  try {
    data = await readFile(path.join(ROOT_PATH, "QoLFix/QoLFix.csproj"), "utf8")
  } catch (e) {
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
  const versionInfo = await getVersionInfo(version, prerelease)

  const pluginFile = path.join(targetFramework, PLUGIN_DLL_NAME)
  const thunderstorePluginFile = path.join(pluginBinPath, CONFIG_RELEASE_THUNDERSTORE, pluginFile)
  const debugPluginFile = path.join(pluginBinPath, CONFIG_DEBUG, pluginFile)
  const standalonePluginFile = path.join(pluginBinPath, CONFIG_RELEASE_STANDALONE, pluginFile)

  const gitDiff = await exec("git diff --shortstat", execOptions)
  const isWorktreeDirty = !!gitDiff.stdout
  const isMasterBranch = versionInfo.branch === "master"

  const promises = [
    // R2ModMan local install package
    createThunderstorePackage(
      path.join(thunderstorePkgPath, `${PKG_NAME.toLowerCase()}-${versionInfo.semver}-r2modman.zip`), {
        pluginFile: debugPluginFile,
        manifest: createR2ModManManifest(versionInfo.semver),
      },
    ),
  ]

  if (!isMasterBranch) {
    logger.warn("The current git branch is not the main branch. Only the r2modman package will be created.")
  } else if (isWorktreeDirty) {
    logger.warn("The git worktree is dirty. Only the r2modman package will be created.")
  } else {
    // Standalone release
    promises.push(copyFile(standalonePluginFile, path.join(standalonePkgPath, PLUGIN_DLL_NAME)))

    // Thunderstore release
    if (prerelease) {
      logger.warn("Thunderstore doesn't yet support SemVer; the Thunderstore package won't be created for this pre-release.")
    } else {
      promises.push(createThunderstorePackage(
        path.join(thunderstorePkgPath, `${PKG_NAME.toLowerCase()}-${versionInfo.semver}-thunderstore.zip`), {
          pluginFile: thunderstorePluginFile,
          manifest: createThunderstoreManifest(versionInfo.semver),
        },
      ))
    }
  }

  await Promise.all(promises)

  logger.info("Done")
}

if (await esMain(import.meta)) {
  await main()
}

export default main
