/***************************************************************************
                          qtscripthandlers.cpp  -  QtScript specific marshallers
                             -------------------
    begin                : 13-07-2008
    copyright            : (C) 2008 by Richard Dale
    email                : richard.j.dale@gmail.com
 ***************************************************************************/

/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU Lesser General Public License as        *
 *   published by the Free Software Foundation; either version 2 of the    *
 *   License, or (at your option) any later version.                       *
 *                                                                         *
 ***************************************************************************/

#include <qyoto.h>
#include <smokeqyoto.h>
#include <marshall_macros.h>

#include <QScriptValue>

DEF_LIST_MARSHALLER( QScriptValueList, QList<QScriptValue*>, QScriptValue )

TypeHandler Qyoto_QtScript_handlers[] = {
    { "QList<QScriptValue>&", marshall_QScriptValueList },
    { "QScriptValueList&", marshall_QScriptValueList },
    { 0, 0 }
};
