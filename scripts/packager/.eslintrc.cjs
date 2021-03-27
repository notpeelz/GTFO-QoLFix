const path = require("path")

const prettierrc = require(path.resolve(__dirname, ".prettierrc.cjs"))

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
    "prettier/prettier": [ "error", prettierrc ],
    // Handled by prettier
    "arrow-body-style": "off",
    "prefer-arrow-callback": "off",

    "object-curly-newline": ["off"],
    "arrow-body-style": ["off"],
    "no-confusing-arrow": ["off"],
    "space-unary-ops": ["off"], // for typeof() expressions (typeof is technically a unary operator)
    "no-control-regex": ["off"],
    "no-underscore-dangle": ["error", {
      allow: ["__dirname", "__filename"],
    }],
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
    "@typescript-eslint/quotes": ["error", "double", {
      avoidEscape: false,
      allowTemplateLiterals: true,
    }],
    "@typescript-eslint/semi": ["error", "always"],
  },
  parser: "@typescript-eslint/parser",
  parserOptions: {
    ecmaVersion: 2021,
    sourceType: "module",
    requireConfigFile: false,
    project: path.resolve(__dirname, "tsconfig.json"),
    extraFileExtensions: [ ".cjs", ".mjs" ],
  },
}
