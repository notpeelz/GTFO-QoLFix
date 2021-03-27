import fs from "fs";
import fsPromises, { readFile, writeFile, mkdir, mkdtemp } from "fs/promises";
import path from "path";
import os from "os";
import { promisify } from "util";
import childProcess from "child_process";
import memoize from "memoizee";
import archiver from "archiver";
import yargs from "yargs";
import * as yargsHelpers from "yargs/helpers";

import createReadme from "./readme";
import Csproj from "./csproj";
import logger from "./logger";
import esMain from "./es-main";
import {
  paths,
  PLUGIN_FILENAME,
  PKG_NAME,
  PKG_AUTHOR,
  PKG_DESCRIPTION,
  PKG_LICENSE,
  REPO_URL,
  PKG_DEPENDENCIES,
} from "./constants";
import {
  PackageConfiguration,
  PluginConfiguration,
  TemplateConfiguration,
} from "./configuration";

const exec = promisify(childProcess.exec);

// eslint-disable-next-line @typescript-eslint/no-shadow
async function rm(path: fs.PathLike, options?: fs.RmOptions): Promise<void> {
  await fsPromises.rm(path, {
    recursive: true,
    force: true,
    maxRetries: 3,
    retryDelay: 100,
    ...options,
  });
}

async function createThunderstoreArtefacts(
  out: fs.PathLike,
  { build, manifest }: { build: PackageBuild; manifest: any },
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
  archive.append(await build.readme(), {
    name: "README.md",
  });
  archive.file(path.join(paths.ROOT, "img/logo.png"), {
    name: "icon.png",
  });

  const plugin = await build.plugin();
  if (typeof plugin === "string") {
    archive.file(plugin, { name: PLUGIN_FILENAME });
  } else {
    archive.append(plugin, { name: PLUGIN_FILENAME });
  }

  await archive.finalize();
}

type VersionInfo = { semver: string; commit: string; branch: string };

async function getVersionInfo(
  version: string,
  prerelease: string,
): Promise<VersionInfo> {
  const r = new RegExp("\n", "g");
  const getStdout = (x: { stdout: string; stderr: string }) =>
    x.stdout.replace(r, "");

  let semver = prerelease ? `${version}-${prerelease}` : version;

  const gitDescribePromise = exec(
    "git describe --long --always --dirty --exclude=* --abbrev=7",
    { cwd: paths.ROOT },
  );
  const gitDescribe = await gitDescribePromise;
  if (gitDescribePromise.child.exitCode !== 0) {
    throw new Error(
      `Failed to get git commit hash: exit code ${gitDescribePromise.child.exitCode}`,
    );
  }

  const gitBranchPromise = exec("git branch --show-current", {
    cwd: paths.ROOT,
  });
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

async function buildPlugin(config: PluginConfiguration): Promise<Buffer> {
  const outputPath = await mkdtemp(path.join(os.tmpdir(), "packager-build"));
  const outputPathEscaped = outputPath.replace(new RegExp('"'), '\\"');
  await exec(`dotnet build -c ${config} -o "${outputPathEscaped}"`, {
    cwd: paths.ROOT,
  });
  const data = await readFile(path.join(outputPath, PLUGIN_FILENAME));
  await rm(outputPath);
  return data;
}

type Packager = (
  versionInfo: VersionInfo,
) => (build: PackageBuild) => Promise<PackageBuild>;

/**
 * Creates a package for Thunderstore releases.
 */
const packageReleaseThunderstore: Packager = ({
  semver,
}: VersionInfo) => async (build: PackageBuild) => {
  const outDir = path.join(paths.OUTPUT, build.configuration);
  await mkdir(outDir, { recursive: true });
  await writeFile(path.join(outDir, "README.md"), await build.readme());
  await writeFile(path.join(outDir, "CHANGELOG.md"), await build.changelog());
  await createThunderstoreArtefacts(
    path.join(outDir, `${PKG_NAME.toLowerCase()}-${semver}-thunderstore.zip`),
    {
      build,
      manifest: {
        name: PKG_NAME,
        description: PKG_DESCRIPTION,
        version_number: semver,
        dependencies: PKG_DEPENDENCIES,
        website_url: REPO_URL,
      },
    },
  );
  return build;
};

/**
 * Creates a local-install package for playtesting.
 */
const packagePlaytest: Packager = ({ semver }: VersionInfo) => async (
  build: PackageBuild,
) => {
  const outDir = path.join(paths.OUTPUT, build.configuration);
  await mkdir(outDir, { recursive: true });
  await writeFile(path.join(outDir, "README.md"), await build.readme());
  await writeFile(path.join(outDir, "CHANGELOG.md"), await build.changelog());
  await writeFile(path.join(outDir, PLUGIN_FILENAME), await build.plugin());
  await createThunderstoreArtefacts(
    path.join(outDir, `${PKG_NAME.toLowerCase()}-${semver}-r2modman.zip`),
    {
      build,
      // This follows the R2ModManPlus ManifestV2 format
      manifest: {
        ManifestVersion: 2,
        AuthorName: PKG_AUTHOR,
        Name: `${PKG_AUTHOR}-${PKG_NAME}`,
        DisplayName: PKG_NAME,
        Version: semver,
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
      },
    },
  );
  return build;
};

/**
 * Creates a package for GitHub releases.
 */
const packageReleaseGitHub: Packager = () => async (build: PackageBuild) => {
  const outDir = path.join(paths.OUTPUT, build.configuration);
  await mkdir(outDir, { recursive: true });
  await writeFile(path.join(outDir, "README.md"), await build.readme());
  await writeFile(path.join(outDir, "CHANGELOG.md"), await build.changelog());
  await writeFile(path.join(outDir, PLUGIN_FILENAME), await build.plugin());
  return build;
};

type PackageBuild = {
  plugin: () => Promise<Buffer | string>;
  configuration: PackageConfiguration;
} & ThenArg<ReturnType<typeof createReadme>>;

const createBuild = async (
  pkgConfig: PackageConfiguration,
  pluginConfig: PluginConfiguration,
  templateConfig: TemplateConfiguration,
): Promise<PackageBuild> => {
  const plugin = memoize(() => buildPlugin(pluginConfig), { promise: true });
  return {
    ...(await createReadme(templateConfig)),
    plugin: async () => plugin(),
    configuration: pkgConfig,
  };
};

async function createPackages() {
  const csproj = await Csproj.fromPath(
    path.join(paths.ROOT, "QoLFix/QoLFix.csproj"),
  );

  const version = csproj.getPropertyValue("Version");
  const prerelease = csproj.getPropertyValue("VersionPrerelease");
  const versionInfo = await getVersionInfo(version, prerelease);

  const builds: {
    [key in keyof typeof PackageConfiguration]: Promise<PackageBuild>;
  } = {
    Thunderstore: createBuild(
      PackageConfiguration.Thunderstore,
      PluginConfiguration.ThunderstoreRelease,
      TemplateConfiguration.Thunderstore,
    ),
    Standalone: createBuild(
      PackageConfiguration.Standalone,
      PluginConfiguration.StandaloneRelease,
      TemplateConfiguration.Standalone,
    ),
    Dev: createBuild(
      PackageConfiguration.Dev,
      PluginConfiguration.Debug,
      TemplateConfiguration.Thunderstore,
    ),
  };

  const logSuccess = (build: PackageBuild) => {
    logger.info(`Created package: ${build.configuration}`);
  };

  const gitDiff = await exec("git diff --shortstat", { cwd: paths.ROOT });
  const isWorktreeDirty = !!gitDiff.stdout;
  const isMasterBranch = versionInfo.branch === "master";

  const promises = [
    builds.Dev.then(packagePlaytest(versionInfo)).then(logSuccess),
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
      builds.Standalone.then(packageReleaseGitHub(versionInfo)).then(
        logSuccess,
      ),
    );

    // Thunderstore release
    if (prerelease) {
      logger.warn(
        "Thunderstore doesn't yet support SemVer; the Thunderstore package won't be created for this pre-release.",
      );
    } else {
      promises.push(
        builds.Thunderstore.then(packageReleaseThunderstore(versionInfo)).then(
          logSuccess,
        ),
      );
    }
  }

  await Promise.all(promises);
}

if (!(await esMain(import.meta.url))) {
  throw new Error("This module is not meant to be imported.");
}

const { argv } = yargs(yargsHelpers.hideBin(process.argv))
  .strict(true)
  .help(true)
  .version(false)
  .option("no-package", {
    alias: "n",
    type: "boolean",
    description:
      "Don't create packages. Only update the root README and CHANGELOG files.",
    default: false,
  });

logger.info("Generating README and CHANGELOG");
const { readme, changelog } = await createReadme(
  TemplateConfiguration.Standalone,
);
await writeFile(path.join(paths.ROOT, "README.md"), await readme());
await writeFile(path.join(paths.ROOT, "CHANGELOG.md"), await changelog());

if (!argv["no-package"]) {
  logger.info("Creating packages");
  await rm(paths.OUTPUT, {
    recursive: true,
    force: true,
    maxRetries: 3,
    retryDelay: 100,
  });
  await createPackages();
}

logger.info("Done");
