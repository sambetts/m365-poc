import { Route } from 'react-router';
import { Layout } from './components/Layout';
import { MainDisplay } from './components/MainDisplay/MainDisplay';


import './custom.css'
import { Routes } from 'react-router-dom';
import { GenerateUrl } from './components/NewMeeting/GenerateUrl';

export default function App() {

    return (
        <Layout>
            <Routes>
                <Route path='/' element={<MainDisplay />} />
                <Route path='/PublishMeeting' element={<GenerateUrl />} />
            </Routes>
        </Layout>
    );
}
