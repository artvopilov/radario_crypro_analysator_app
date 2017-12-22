const React = require('react');
const PropTypes = require('prop-types');

const PairInfo = require('./PairInfo');


class ActualPairs extends React.Component {
    constructor(props) {
        super(props);
    }
// пара - покупка - продажа - спред - специальные отметки
    render() {
        return (
            <table className="pairsAndCrosses">
                <tr>
                    <th className="pair">Пара</th>
                    <th className="buy">Покупка</th>
                    <th className="sell">Продажа</th>
                    <th className="spread">Спред</th>
                    <th className="special">Спец отметки</th>
                    <th className="relevance">Актуальность</th>
                </tr>
                <tbody>
                    {this.props.data.map(curPair => {
                        return (
                            <PairInfo pair={curPair.pair} seller={curPair.stockExchangeSeller} buyer={curPair.stockExchangeBuyer}
                                      purchasePrice={curPair.purchasePrice} sellPrice={curPair.sellPrice} isCross={this.props.areCrosses}
                                      url={`/api/actualpairs/${curPair.pair}?seller=${curPair.stockExchangeSeller.toLowerCase()}` +
                                    `&buyer=${curPair.stockExchangeBuyer.toLowerCase()}` +
                                    `&isCross=${this.props.areCrosses ? "true" : "false"}`}/>
                        )
                    })}
                </tbody>
            </table>
        )
    }
}

ActualPairs.proptypes = {
    data: PropTypes.array,
    areCrosses: PropTypes.bool
};


module.exports = ActualPairs;
