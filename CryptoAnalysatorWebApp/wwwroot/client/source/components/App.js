const React = require('react');
const axios = require('axios');

const StatusBar = require('./StatusBar');
const ActualPairs = require('./ActualPairs');
const Tools = require('./Tools');

class App extends React.Component {
    constructor(props) {
        super(props);

        this.state = {
            crosses: [],
            pairs: [],
            filter: 0
        };
    }


    componentDidMount() {
        this.updatePairs(null);
    }

    updatePairs(event) {
        let btn;
        if (event !== null) {
            event.preventDefault();
            btn = event.target.lastChild;
            btn.innerText = "Loading...";
            btn.classList.add('loading');
        }
        axios.get('/api/actualpairs/')
            .then((response) => {
                if (event !== null) {
                    btn.innerText = "Update";
                    btn.classList.remove('loading');
            }
                const pairs = response.data.pairs;
                const crosses = response.data.crosses;

                this.setState({pairs, crosses});
            });
    }

    onChangeFilter(event) {
        this.setState({
            filter: event.target.value
        })
    }

    render() {
        return (
                <div id="app">
                    <StatusBar/>
                    <Tools onChangeFilter={this.onChangeFilter.bind(this)} updatePairs={this.updatePairs.bind(this)}/>
                    <ActualPairs data={this.state.pairs} areCrosses={false} filter={this.state.filter}/>
                    <ActualPairs data={this.state.crosses} areCrosses={true} filter={this.state.filter}/>
                </div>
        )
    }
}



module.exports = App;
