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
            <ul className="pairsAndCrosses">
                {this.props.data.map(curPair => {
                    return (
                        <li>
                            <PairInfo pair={curPair.pair} seller={curPair.stockExchangeSeller} buyer={curPair.stockExchangeBuyer}
                                      purchasePrice={curPair.purchasePrice} sellPrice={curPair.sellPrice} isCross={this.props.areCrosses}
                                      url={`/api/actualpairs/${curPair.pair}?seller=${curPair.stockExchangeSeller.toLowerCase()}` +
                                      `&buyer=${curPair.stockExchangeBuyer.toLowerCase()}` +
                                      `&isCross=${this.props.areCrosses ? "true" : "false"}`}/>
                        </li>
                    )
                })}
            </ul>
        )
    }
}

ActualPairs.proptypes = {
    data: PropTypes.array,
    areCrosses: PropTypes.bool
};


module.exports = ActualPairs;
