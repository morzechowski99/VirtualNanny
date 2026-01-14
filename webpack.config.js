const path = require('path');
const { CleanWebpackPlugin } = require('clean-webpack-plugin');

module.exports = {
  entry: './scripts-src/index.ts',
  output: {
    filename: 'bundle.js',
    path: path.resolve(__dirname, 'dist'),
    library: 'VirtualNanny',
    libraryTarget: 'umd',
    globalObject: 'typeof self !== "undefined" ? self : this'
  },
  module: {
    rules: [
      {
        test: /\.ts$/,
        use: 'ts-loader',
        exclude: /node_modules/
      }
    ]
  },
  resolve: {
    extensions: ['.ts', '.js'],
    alias: {
      '@': path.resolve(__dirname, 'scripts-src/')
    }
  },
  plugins: [
    new CleanWebpackPlugin()
  ],
  devtool: 'source-map',
  devServer: {
    static: {
      directory: path.join(__dirname, 'wwwroot')
    },
    compress: true,
    port: 3000,
    hot: true
  }
};
