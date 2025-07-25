import {StrictMode} from 'react';
import {createRoot} from 'react-dom/client';
import './index.css';
import {createBrowserRouter, RouterProvider} from "react-router-dom";
import {Layout} from "@/layout.tsx";
import {RegisterPage} from "@/pages/register-page.tsx";
import {LoginPage} from "@/pages/loginPage.tsx";
import {RegistrationSuccessPage} from "@/pages/registrationSuccessPage.tsx";
import {ProtectedRoute} from "@/protectedRoute.tsx";
import {HomePage} from "@/pages/homePage.tsx";
import {App} from "@/app.tsx";
import {PublicRoute} from "@/publicRoute.tsx";
import {SchedulePage} from "@/pages/schedule-page.tsx";
import {TelegramBotPage} from "@/pages/telegramBotPage.tsx";
import {ErrorPage} from "@/pages/errorPage.tsx";
import {ApproveMessagesPage} from "@/pages/approveMessagesPage.tsx";
import {ParseChannelPage} from "@/pages/parse-channel-page.tsx";
import {MessagesPage} from "@/pages/messages-page.tsx";

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
