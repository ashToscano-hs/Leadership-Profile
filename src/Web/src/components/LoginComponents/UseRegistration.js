import { useState, useEffect } from 'react';
import { useHistory } from "react-router-dom";

function UseRegistration() {
    const history = useHistory();

    const [username, setUsername] = useState('');
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [staffUniqueId, setStaffUniqueId] = useState('');
    const [registrationInfo, setRegistrationInfo] = useState({
        username: username,
        password: password,
        email: email,
        staffUniqueId: staffUniqueId,
    });
    const [error, setError] = useState({
        hasError: false,
        message: ''
    });

    useEffect(() => {
        if (password === confirmPassword) {
            setError({
                hasError: false,
                message: ''
            });
            setRegistrationInfo({
                username: username,
                password: password,
                email: email,
                staffUniqueId: staffUniqueId,
            });
        } else {
            setError({
                hasError: true,
                message: 'Passwords must match.'
            });
        }
    }, [username, email, password, confirmPassword, staffUniqueId]);

    async function setRegistration() {
        let unmounted = false;
        const apiUrl = new URL('/account/register', new URL(process.env.REACT_APP_API_URL));
        fetch(apiUrl, {
            method: 'POST',
            mode: 'no-cors',
            credentials: 'include',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'text/plain',
            },
            referrerPolicy: 'origin-when-cross-origin',
            body: JSON.stringify(registrationInfo)
        }).then((response) => {
            if (!unmounted && response !== null) {
                setError(false);
                history.push('/queue?count=10&page=1&sortBy=desc&sortField=id');
                history.go(0);
            }
        }).catch((error) => {
            const errorInfo = error.response.data;
            if (errorInfo.message === undefined) {
                const errors = Object.values(errorInfo.errors).map(error => error.join(" ")).join(" ")
                setError({
                    hasError: true,
                    message: errors
                });
            } else {
                setError({
                    hasError: true,
                    message: errorInfo.message
                });
            }
            console.error(error.message);
        });
        return () => {
            unmounted = true;
        };
    }

    return {
        setRegistration,
        error,
        bind: {
            registrationInfo,
            onChange: event => {
                switch (event.target.name) {
                    case 'username':
                        setUsername(event.target.value);
                        break;
                    case 'email':
                        setEmail(event.target.value);
                        break;
                    case 'password':
                        setPassword(event.target.value);
                        break;
                    case 'confirmPassword':
                        setConfirmPassword(event.target.value);
                        break;
                    case 'staffUniqueId':
                        setStaffUniqueId(event.target.value);
                        break;
                    default:
                        console.error('No matching elements');
                }
            }
        },
    }
}

export default UseRegistration