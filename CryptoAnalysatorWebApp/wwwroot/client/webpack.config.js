const path = require('path');
const ExtractTextPlugin = require('extract-text-webpack-plugin');


module.exports = {
    entry: './source/index.js',
    module: {
        rules: [
            {
                test: /.js$/,
                include: /source/,
                exclude: /node_modules/,
                use: 'babel-loader'
            },
            {
                test: /.css$/,
                include: /public/,
                loader: ExtractTextPlugin.extract({
                    fallback: 'style-loader',
                    use: 'css-loader'
                })
            },
            {
                test: /\.(jpe?g|png|gif|svg)$/i,
                loaders: [
                    'file-loader?name=images/[name].[ext]'
                ]
            }
        ]
    },
    output: {
        path: path.resolve(__dirname, 'dist'),
        filename: 'index.bundle.js'
    },
    plugins: [
        new ExtractTextPlugin('bundle.css')
    ]
};
