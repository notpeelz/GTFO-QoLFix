import fs from "fs";
import { copyFile, mkdir } from "fs/promises";
import path from "path";
import { promisify } from "util";
import childProcess from "child_process";
import archiver from "archiver";
import Csproj from "../csproj";

import logger from "../logger";
import {
  paths,
  PKG_NAME,
  PKG_AUTHOR,
  PKG_DESCRIPTION,
  PKG_LICENSE,
  REPO_URL,
  PKG_DEPENDENCIES,
} from "../constants";

const exec = promisify(childProcess.exec);

const PLUGIN_DLL_NAME = `${PKG_NAME}.dll`;
const CONFIG_RELEASE_STANDALONE = "Release-Standalone";
const CONFIG_RELEASE_THUNDERSTORE = "Release-Thunderstore";
const CONFIG_DEBUG = "Debug";

const pluginBinPath = path.join(paths.ROOT, "QoLFix/bin");

const execOptions = { cwd: paths.ROOT };

function createR2ModManManifest(version: string) {
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
  };
}

function createThunderstoreManifest(version: string) {
  return {
    name: PKG_NAME,
    description: PKG_DESCRIPTION,
    version_number: version,
    dependencies: PKG_DEPENDENCIES,
    website_url: REPO_URL,
  };
}

async function createThunderstorePackage(
  out: fs.PathLike,
  { pluginFile, manifest }: { pluginFile: string; manifest: any },
) {
  const output = fs.createWriteStream(out);

  const archive = archiver("zip");

  archive.on("error", (err) => {
    throw err;
  });
  archive.pipe(output);

  archive.append(Buffer.from(JSON.stringify(manifest, null, 2)), {
    name: "manifest.json",
  });
  archive.file(path.join(paths.ROOT, "img/logo.png"), {
    name: "icon.png",
  });
  archive.file(path.join(paths.OUTPUT_THUNDERSTORE, "README.md"), {
    name: "README.md",
  });
  archive.file(pluginFile, { name: "QoLFix.dll" });

  await archive.finalize();
}

async function getVersionInfo(
  version: string,
  prerelease: string,
): Promise<{ semver: string; commit: string; branch: string }> {
  const r = new RegExp("\n", "g");
  const getStdout = (x: { stdout: string; stderr: string }) =>
    x.stdout.replace(r, "");

  let semver = prerelease ? `${version}-${prerelease}` : version;

  const gitDescribePromise = exec(
    "git describe --long --always --dirty --exclude=* --abbrev=7",
    execOptions,
  );
  const gitDescribe = await gitDescribePromise;
  if (gitDescribePromise.child.exitCode !== 0) {
    throw new Error(
      `Failed to get git commit hash: exit code ${gitDescribePromise.child.exitCode}`,
    );
  }

  const gitBranchPromise = exec("git branch --show-current", execOptions);
  const gitBranch = await gitBranchPromise;
  if (gitBranchPromise.child.exitCode !== 0) {
    throw new Error(
      `Failed to get git branch: exit code ${gitBranchPromise.child.exitCode}`,
    );
  }

  const commit = getStdout(gitDescribe);
  const branch = getStdout(gitBranch);

  semver += `+git${commit}-${branch}`;

  return {
    semver,
    commit,
    branch,
  };
}

export default async function main() {
  logger.info("Building");
  await Promise.all([
    exec(`dotnet build -c ${CONFIG_DEBUG}`, execOptions),
    exec(`dotnet build -c ${CONFIG_RELEASE_STANDALONE}`, execOptions),
    exec(`dotnet build -c ${CONFIG_RELEASE_THUNDERSTORE}`, execOptions),
  ]);

  logger.info("Packaging");
  await mkdir(paths.OUTPUT_THUNDERSTORE, { recursive: true });
  await mkdir(paths.OUTPUT_STANDALONE, { recursive: true });

  const csproj = await Csproj.fromPath(
    path.join(paths.ROOT, "QoLFix/QoLFix.csproj"),
  );

  const targetFramework = csproj.getPropertyValue("TargetFramework");

  const version = csproj.getPropertyValue("Version");
  const prerelease = csproj.getPropertyValue("VersionPrerelease");
  const versionInfo = await getVersionInfo(version, prerelease);

  const pluginFile = path.join(targetFramework, PLUGIN_DLL_NAME);
  const thunderstorePluginFile = path.join(
    pluginBinPath,
    CONFIG_RELEASE_THUNDERSTORE,
    pluginFile,
  );
  const debugPluginFile = path.join(pluginBinPath, CONFIG_DEBUG, pluginFile);
  const standalonePluginFile = path.join(
    pluginBinPath,
    CONFIG_RELEASE_STANDALONE,
    pluginFile,
  );

  const gitDiff = await exec("git diff --shortstat", execOptions);
  const isWorktreeDirty = !!gitDiff.stdout;
  const isMasterBranch = versionInfo.branch === "master";

  const promises = [
    // R2ModMan local install package
    createThunderstorePackage(
      path.join(
        paths.OUTPUT_THUNDERSTORE,
        `${PKG_NAME.toLowerCase()}-${versionInfo.semver}-r2modman.zip`,
      ),
      {
        pluginFile: debugPluginFile,
        manifest: createR2ModManManifest(versionInfo.semver),
      },
    ),
  ];

  if (!isMasterBranch) {
    logger.warn(
      "The current git branch is not the main branch. Only the r2modman package will be created.",
    );
  } else if (isWorktreeDirty) {
    logger.warn(
      "The git worktree is dirty. Only the r2modman package will be created.",
    );
  } else {
    // Standalone release
    promises.push(
      copyFile(
        standalonePluginFile,
        path.join(paths.OUTPUT_STANDALONE, PLUGIN_DLL_NAME),
      ),
    );

    // Thunderstore release
    if (prerelease) {
      logger.warn(
        "Thunderstore doesn't yet support SemVer; the Thunderstore package won't be created for this pre-release.",
      );
    } else {
      promises.push(
        createThunderstorePackage(
          path.join(
            paths.OUTPUT_THUNDERSTORE,
            `${PKG_NAME.toLowerCase()}-${versionInfo.semver}-thunderstore.zip`,
          ),
          {
            pluginFile: thunderstorePluginFile,
            manifest: createThunderstoreManifest(versionInfo.semver),
          },
        ),
      );
    }
  }

  await Promise.all(promises);

  logger.info("Done");
}
