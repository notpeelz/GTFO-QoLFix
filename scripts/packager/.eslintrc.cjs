const path = require("path");
const airbnbStyleRules = require("eslint-config-airbnb-base/rules/style");

const prettierrc = require(path.resolve(__dirname, ".prettierrc.cjs"));

module.exports = {
  root: true,
  env: {
    node: true,
  },
  ignorePatterns: [
    ".eslintrc.cjs",
    "rollup.config.mjs",
  ],
  extends: [
    "airbnb-typescript/base",
    "prettier",
  ],
  plugins: [
    "prettier",
  ],
  settings: {
    "import/extensions": [
      ".js",
      ".cjs",
      ".mjs",
    ],
    "import/ignore": [
      "fs/promises",
    ],
  },
  rules: {
    "prettier/prettier": ["error", prettierrc],
    // Handled by prettier
    "arrow-body-style": "off",
    "prefer-arrow-callback": "off",

    "object-curly-newline": ["off"],
    "arrow-body-style": ["off"],
    "no-confusing-arrow": ["off"],
    "no-control-regex": ["off"],
    "no-underscore-dangle": ["error", {
      allow: ["__dirname", "__filename"],
    }],
    "no-restricted-syntax": ["error", ...airbnbStyleRules.rules["no-restricted-syntax"]
      .filter(x => {
        if (typeof x !== "object") return false;
        if (x.selector === "ForOfStatement") return false;
        return true;
      })
    ],
    "@typescript-eslint/naming-convention": [
      "error",
      {
        selector: "variable",
        format: [ "camelCase", "UPPER_CASE", "PascalCase" ],
        filter: {
          regex: "__dirname|__filename",
          match: false
        },
      },
      {
        selector: "function",
        format: [ "camelCase", "PascalCase" ],
      },
      {
        selector: "typeLike",
        format: [ "PascalCase" ],
      },
    ],
  },
  parser: "@typescript-eslint/parser",
  parserOptions: {
    ecmaVersion: 2021,
    sourceType: "module",
    requireConfigFile: false,
    project: path.resolve(__dirname, "tsconfig.json"),
    extraFileExtensions: [ ".cjs", ".mjs" ],
  },
};
