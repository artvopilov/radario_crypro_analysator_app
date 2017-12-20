const React = require('react');


class Header extends React.Component {
    constructor(props) {
        super(props)
    }

    render() {
        return (
            <ul className="header">
                <li className="pairName">Пара</li>
                <li className="buy">Покупка</li>
                <li className="sell">Продажа</li>
                <li className="spread">Спред</li>
                <li className="special">Спец отметки</li>
                <li className="relevance">Актуальность</li>
            </ul>
        )
    }
}


module.exports = Header ;
