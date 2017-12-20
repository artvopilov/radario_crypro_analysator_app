const React = require('react');
const axios = require('axios');

const Header = require('./Header');
const ActualPairs = require('./ActualPairs');

class App extends React.Component {
    constructor(props) {
        super(props);

        this.state = {
            crosses: [],
            pairs: []
        };
    }


    componentDidMount() {
        axios.get('/api/actualpairs')
            .then((response) => {
                const pairs = response.data.pairs;
                const crosses = response.data.crosses;
                this.setState({pairs, crosses});
            });
    }

    render() {
        return (
            <div id="app">
                <Header/>
                <ActualPairs data={this.state.pairs} areCrosses={false}/>
                <ActualPairs data={this.state.crosses} areCrosses={true}/>
            </div>
        )
    }
}



module.exports = App;
