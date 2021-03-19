module.exports = function(api) {
  api.cache(false)
  return {
    plugins: ["@babel/plugin-syntax-top-level-await"],
  }
}
