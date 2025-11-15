import {StrictMode} from 'react';
import {createRoot} from 'react-dom/client';
import './index.css';
import {createBrowserRouter, RouterProvider} from "react-router-dom";
import {Layout} from "@/layout.tsx";
import {RegisterPage} from "@/pages/register-page.tsx";
import {LoginPage} from "@/pages/login-page.tsx";
import {RegistrationSuccessPage} from "@/pages/registration-success-page.tsx";
import {ProtectedRoute} from "@/protected-route.tsx";
import {HomePage} from "@/pages/home-page.tsx";
import {App} from "@/app.tsx";
import {PublicRoute} from "@/public-route.tsx";
import {TelegramBotPage} from "@/pages/telegram-bot-page.tsx";
import {ErrorPage} from "@/pages/error-page.tsx";
import {ApproveMessagesPage} from "@/pages/approve-messages-page.tsx";
import {ParseChannelPage} from "@/pages/parse-channel-page.tsx";
import {MessagesPage} from "@/pages/messages-page.tsx";
import {LogOutPage} from "@/pages/logout-page.tsx";
import {SchedulePage} from "@/pages/schedule-page.tsx";

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
                    }
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
                            {
                                path: "/schedule",
                                element: <SchedulePage/>,
                            },
                            {
                                path: "/telegram-bot",
                                element: <TelegramBotPage/>
                            },
                            {
                                path: "/approve-messages",
                                element: <ApproveMessagesPage/>
                            },
                            {
                                path: "/messages",
                                element: <MessagesPage/>
                            },
                            {
                                path: "/parse-channel",
                                element:  <ParseChannelPage/>
                            },
                            {
                                path: "/logout",
                                element: <LogOutPage/>,
                            }
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
