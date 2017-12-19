const React = require('react');
const ReactDom = require('react-dom');
const App = require('./components/App');

import '../public/styles/main.css'


ReactDom.render(
    <App/>,
    document.getElementById('app')
);
