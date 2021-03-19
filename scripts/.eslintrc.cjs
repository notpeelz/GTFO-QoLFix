const path = require("path")

module.exports = {
  env: {
    node: true,
  },
  extends: [
    "airbnb-base",
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
    "import/no-unresolved": ["error", {
      ignore: [
        "fs/promises",
      ],
    }],
    "object-curly-newline": ["off"],
    "arrow-body-style": ["off"],
    "no-confusing-arrow": ["off"],
    "space-unary-ops": ["off"], // for typeof() expressions
    "no-control-regex": ["off"],
    "no-underscore-dangle": ["error", {
      allow: ["__dirname", "__filename"],
    }],
    quotes: ["error", "double", {
      avoidEscape: false,
      allowTemplateLiterals: true,
    }],
    semi: ["error", "never"],
  },
  parser: "@babel/eslint-parser",
  parserOptions: {
    ecmaVersion: 2021,
    sourceType: "module",
    requireConfigFile: false,
    babelOptions: {
      configFile: path.resolve(path.join(__dirname, ".babelrc.cjs")),
    },
  },
}
