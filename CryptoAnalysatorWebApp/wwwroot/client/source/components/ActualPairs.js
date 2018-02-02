const React = require('react');
const PropTypes = require('prop-types');

const PairInfo = require('./PairInfo');


class ActualPairs extends React.Component {
    constructor(props) {
        super(props);
    }

    render() {
        return (
            <table className="pairsAndCrosses">
                <tr>
                    <th className="pair">Пара</th>
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
                            <PairInfo pair={curPair.pair} seller={curPair.stockExchangeSeller} buyer={curPair.stockExchangeBuyer} spread={curPair.spread}
                                      purchasePrice={curPair.purchasePrice} sellPrice={curPair.sellPrice} isCross={curPair.isCross}
                                      url={`/api/actualpairs/${curPair.pair}?seller=${curPair.stockExchangeSeller.toLowerCase()}` +
                                    `&buyer=${curPair.stockExchangeBuyer.toLowerCase()}` +
                                    `&isCross=${curPair.isCross ? "true" : "false"}`}/>
                        )
                    })}
                </tbody>
            </table>
        )
    }
}

ActualPairs.proptypes = {
    data: PropTypes.array,
    filter: PropTypes.number
};


module.exports = ActualPairs;
