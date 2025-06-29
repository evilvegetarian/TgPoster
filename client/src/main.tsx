import {StrictMode} from 'react';
import {createRoot} from 'react-dom/client';
import './index.css';
import {createBrowserRouter, RouterProvider} from "react-router-dom";
import ErrorPage from "@/pages/errorPage.tsx";
import {Layout} from "@/layout.tsx";
import {RegisterPage} from "@/pages/registerPage.tsx";
import {LoginPage} from "@/pages/loginPage.tsx";
import {RegistrationSuccessPage} from "@/pages/registrationSuccessPage.tsx";
import {ProtectedRoute} from "@/protectedRoute.tsx";
import {HomePage} from "@/pages/homePage.tsx";
import {App} from "@/app.tsx";
import {PublicRoute} from "@/publicRoute.tsx";

const router = createBrowserRouter([
    {
        element: <App/>,
        errorElement: <ErrorPage/>,
        children: [
            {
                element: <PublicRoute/>,
                children: [
                    {
                        path: "/login",
                        element: <LoginPage/>,
                    },
                    {
                        path: "/register",
                        element: <RegisterPage/>,
                    },
                    {
                        path: "/register/success",
                        element: <RegistrationSuccessPage/>,
                    },
                ]
            },
            {
                element: <ProtectedRoute/>,
                children: [
                    {
                        path: "/",
                        element: <Layout/>,
                        children: [
                            {
                                index: true,
                                element: <HomePage/>,
                            },
                        ]
                    }
                ]
            }
        ]
    }
]);

const rootElement = document.getElementById('root')!;
const root = createRoot(rootElement);

root.render(
    <StrictMode>
        <RouterProvider router={router}/>
    </StrictMode>,
);
