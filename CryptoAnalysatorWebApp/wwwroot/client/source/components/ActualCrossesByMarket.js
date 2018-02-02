const React = require('react');
const PropTypes = require('prop-types');

const CrossInfo = require('./CrossInfo');


class ActualCrossesByMarket extends React.Component {
    constructor(props) {
        super(props);
    }

    render() {
        return (
            <table className="pairsAndCrosses">
                <tr>
					<th className="market">Биржа</th>
                    <th className="buy">Покупка</th>
                    <th className="sell">Продажа</th>
                    <th className="spread">Профит</th>
                    <th className="special">Спец отметки</th>
                    <th className="relevance">Актуальность</th>
                </tr>
                <tbody>
                    {this.props.data.filter(curPair => {
                        return curPair.spread >= this.props.filter
                    }).map(curPair => {
                        return (
                            <CrossInfo market={curPair.market} purchasePath={curPair.purchasePath} sellPath={curPair.sellPath} spread={curPair.spread}
                                      purchasePrice={curPair.purchasePrice} sellPrice={curPair.sellPrice} isCross={curPair.isCross}
                                      url={`/api/actualpairs/crossMarket/${curPair.market}?purchasepath=${curPair.purchasePath}&sellpath=${curPair.sellPath}`}/>
                        )
                    })}
                </tbody>
            </table>
        )
    }
}

ActualCrossesByMarket.proptypes = {
    data: PropTypes.array,
    areCrosses: PropTypes.bool,
    filter: PropTypes.number
};


module.exports = ActualCrossesByMarket;